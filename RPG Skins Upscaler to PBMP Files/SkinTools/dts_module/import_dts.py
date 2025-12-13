
import bpy
from dts_module import dts
import mathutils
from .util import default_materials, resolve_texture, get_rgb_colors
from itertools import zip_longest, count
BLENDER_FPS = 24

import pathlib
module_dir = pathlib.Path(__file__).resolve().parent

class ImportParams:
    def __init__(self,mat_flag,palette):
        self.import_mat_flag = mat_flag
        self.palette_file = palette

def grouper(iterable, n, fillvalue=None):
    "Collect data into fixed-length chunks or blocks"
    # grouper('ABCDEFG', 3, 'x') --> ABC DEF Gxx"
    args = [iter(iterable)] * n
    return zip_longest(*args, fillvalue=fillvalue)

def dedup_name(group, name):
    if name not in group:
        return name

    for suffix in count(2):
        new_name = name + "#" + str(suffix)

        if new_name not in group:
            return new_name

def ShowMessageBox(message = "", title = "Message Box", icon = 'INFO'):

    def draw(self, context):
        self.layout.label(text=message)

    bpy.context.window_manager.popup_menu(draw, title = title, icon = icon)


def convert_texture_image(filepath,pal,pal_file):
    from PIL import Image
    import os

    from . import tribes_bmp
    
    tbmp = tribes_bmp.Tribes_BMP()
    print(filepath)
    bmp_type = tbmp.load_pbmp(filepath)
    pal_index = tbmp.pal_index
    out_folder = os.path.join(os.path.dirname(filepath),"fixed_textures")
    imm_file = None
    if bmp_type == "pbmp":
        from . import tribes_pal
        pal_data = tribes_pal.Tribes_PPL(file_name=pal_file)
        imm_file = os.path.join(out_folder,pathlib.Path(filepath).stem + ".png")
        tbmp.save_png(imm_file,pal_data)
        return imm_file
    mesh_tex_image = Image.open(filepath)
    pal_decoder_file = "%s%d.bmp" % (pal,pal_index)
    pal_decoder_path = os.path.join(os.path.join(module_dir,"palette_data"),pal_decoder_file)
    pal_decoder_image = Image.open(pal_decoder_path)
    mesh_tex_image_decoded = Image.new('RGB',(mesh_tex_image.width,mesh_tex_image.height),"black")

    for ii in range(mesh_tex_image.width):
        for jj in range(mesh_tex_image.height):
            pixVal = mesh_tex_image.getpixel((ii,jj))
            color = pal_decoder_image.getpixel((pixVal,0))
            mesh_tex_image_decoded.putpixel((ii,jj),color)

    
    if not os.path.exists(out_folder):
        os.makedirs(out_folder)
    tex_filename = os.path.basename(filepath)
    out_file = os.path.join(out_folder,tex_filename)
    mesh_tex_image_decoded.save(out_file)
    mesh_tex_image_decoded.close()
    pal_decoder_image.close()
    mesh_tex_image.close()
    return out_file

def import_material(color_source, dmat, filepath,palette):
    bmat = bpy.data.materials.new(dedup_name(bpy.data.materials, dmat.params.fileName))
    pal_prefix = pathlib.Path(palette).stem
    #print(dmat.params.fileName)
    #bmat.diffuse_intensity = 1
    bmat.use_nodes = True
    bsdf = bmat.node_tree.nodes["Principled BSDF"]
    image_path = resolve_texture(filepath, dmat.params.fileName)
    if image_path:
        texname = convert_texture_image(image_path,pal_prefix,palette)
        #texname = resolve_texture(filepath, dmat.params.fileName)
        
        #print(texname)
        if texname is not None:
            try:
                teximg = bpy.data.images.load(texname)
            except:
                print("Cannot load image", texname)

            tex = bpy.data.textures.new(dmat.params.fileName, "IMAGE")
            tex.image = teximg
            
            texture_node = bmat.node_tree.nodes.new('ShaderNodeTexImage')
            
            texture_node.image = teximg
            bmat.node_tree.links.new(texture_node.outputs['Color'],bsdf.inputs['Base Color'])

            #for index in bmat.node_tree.nodes:
                #print(index)
        #Apply texture props here
        return bmat
    else:
        return None
    
# nodes and objects have same variable name for the first subsequence
def add_all_keyframes(model, seq_start_keyframe, bobj, tobj, obj_index):
    current_keyframe = 0
    num_sub = tobj.num_sub_seq
    first_sub_index = tobj.first_sub_seq
    for i in range(0, num_sub):
        sub_seq = model.sub_sequences[first_sub_index + i]
        seq = model.sequences[sub_seq.sequence_idx]  # getting the name of the animation
        seq_name = str(model.names[seq.name_index]).split('\\x00')[0][2:]
        start_keyframe = seq_start_keyframe[sub_seq.sequence_idx]
        duration = seq.duration

        start_kf = sub_seq.first_key_frame
        for keyframe_index in range(start_kf, start_kf + sub_seq.num_key_frames):
            keyframe = model.keyframes[keyframe_index]
            frame_num = start_keyframe + keyframe.position * duration * BLENDER_FPS

            transform = model.transforms[keyframe.key_value]
            bobj.location = transform.translate
            bobj.rotation_mode = "QUATERNION"

            (w, x, y, z) = transform.rotate.get_quatwxyz()
            bobj.rotation_quaternion = (-w, x, y, z)
            bobj.scale = (transform.scale, transform.scale, transform.scale)

            bobj.keyframe_insert(data_path="location", frame=frame_num)
            bobj.keyframe_insert(data_path="rotation_quaternion", frame=frame_num)

def load(operator, context, filepath,options : ImportParams,
         import_sequences=True,
         debug_report=False):
         #palette_prefix="lush.day"):
    model = dts.dts()
    model.load_file(filepath)

    # Create a Blender material for each DTS material
    materials = {}
    if options.import_mat_flag:
        color_source = get_rgb_colors()
        notifStr = ""
        errorFlag = False
        for mat in model.material_list.matList:
            materials[mat] = import_material(color_source,mat,filepath,options.palette_file)
            
            if not materials[mat]:
                notifStr = notifStr + f"{mat.params.fileName}: ERROR: Cannot Locate File | "
                errorFlag = True
        if errorFlag:
            msgType = 'ERROR'
            ShowMessageBox(notifStr,"Material Load Status",msgType)
        
    #for key,value in materials.items():
    #    print(key.params.fileName,value)
        
    # The trick is that there is a root node that has everything linked to it.  So we can get around that structure by creating node objects
    # with no meshes and setting the parent to the object
    bnodes = []
    for i in range(0, model.num_nodes):
        node = model.nodes[i]
        name = str(model.names[node.name_index]).split('\\x00')[0][2:]
        bnode = bpy.data.objects.new(f'node_{name}', None)
        transform = model.transforms[node.transform_index]

        if node.parent_node != i and node.parent_node != -1:
            bnode.parent = bnodes[node.parent_node]

        bnode.location = transform.translate
        bnode.rotation_mode = "QUATERNION"
        (w, x, y, z) = transform.rotate.get_quatwxyz()
        bnode.rotation_quaternion = (-w, x, y, z)
        bnode.scale = (transform.scale, transform.scale, transform.scale)

        bnodes.append(bnode)

        bpy.context.collection.objects.link(bnode)
        bnode.keyframe_insert(data_path="location", frame=0)
    # Create the objects and assign them to the nodes
    bobjects = []
    for i in range(0, model.num_objects):
        object = model.objects[i]
        name = str(model.names[object.name]).split('\\x00')[0][2:]

        mesh = model.meshes[object.mesh_index]

        scale = mesh.frames[0].scale
        origin = mesh.frames[0].origin
        vertices = []
        edges = []
        faces = []
        uvs = []
        dmat_indexes = []
        for vert in mesh.verts:
            vert_point = vert.get_unpacked_vert(scale, origin)
            vert_normal = vert.get_normal()
            vertices.append(vert_point)
            
        
        for face in mesh.faces:
            faces.append((face.vert_index0, face.vert_index1, face.vert_index2))
            uvs += [mesh.text_verts[face.tex_index0][0], 1 - mesh.text_verts[face.tex_index0][1]]
            uvs += [mesh.text_verts[face.tex_index1][0], 1 - mesh.text_verts[face.tex_index1][1]]
            uvs += [mesh.text_verts[face.tex_index2][0], 1 - mesh.text_verts[face.tex_index2][1]]
            if face.mat_index not in dmat_indexes:
                dmat_indexes.append(face.mat_index)
                
                
        bmesh = bpy.data.meshes.new(name)
        bmesh.from_pydata(vertices, edges, faces)
        if options.import_mat_flag:
            for dind in dmat_indexes: #Material indexes are found via faces
                #dmat_name = model.material_list.matList[dind].params.fileName
                bmat = materials[model.material_list.matList[dind]]
                if bmat:
                    bmesh.materials.append(bmat)
                #print(f"Mesh: {name} - Mat: {dmat_name} - Face_Ind: {dind} - MatObj: {bmat}")
        bmesh.update()

        #print(uvs);
        boject_uv = bmesh.uv_layers.new(name=f'{name}_uv')
        boject_uv.data.foreach_set("uv", uvs)
        bmesh.update()
        bobj = bpy.data.objects.new(f'{name}_obj', bmesh)

        bobj.location = object.offset
        node_index = object.node_index

        bobj.parent = bnodes[node_index]
        #material_indices = {}
        #for mm in model.material_list.matList:
        #    if mm not in material_indices:
        #        #material_indices[mm] = len(mm)
        #        bmesh.materials.append(materials[mm])
        
        bpy.context.collection.objects.link(bobj)
        bobjects.append(bobj)
        bobj.keyframe_insert(data_path="location", frame=0)

    if import_sequences:
        seq_start_keyframe = []
        curr_keyframe = 0
        for seq in model.sequences:
            seq_start_keyframe.append(curr_keyframe)
            curr_keyframe += seq.duration * BLENDER_FPS

        for i in range(0, model.num_nodes):
            add_all_keyframes(model, seq_start_keyframe, bnodes[i], model.nodes[i], i)

    return {"FINISHED"}
