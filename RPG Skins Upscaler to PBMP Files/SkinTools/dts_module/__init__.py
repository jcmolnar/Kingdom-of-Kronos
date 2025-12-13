bl_info = {
    "name": "DTS format",
    "author": "Bovidi and Kyred",
    "version": (0, 2, 0),
    "blender": (4, 0, 0),
    "location": "File > Import-Export",
    "description": "Import-Export DTS, Import DTS mesh, UV's, "
                   "materials and textures",
    "warning": "",
    "support": 'COMMUNITY',
    "category": "Import-Export"}

if "bpy" in locals():
    import importlib
    if "import_dts" in locals():
        importlib.reload(import_dts)
    #if "export_dts" in locals():
    #    importlib.reload(export_dts)

import pkg_resources
installed_packages_list = sorted(["%s" % (i.key)
   for i in pkg_resources.working_set])

if "numpy" not in installed_packages_list:
    import pip
    pip.main(['install','numpy','--user'])
    
if "pillow" not in installed_packages_list:
    import pip
    pip.main(['install','pillow','--user'])

is_developer = False
try:
    from .developer import is_developer
except ImportError:
    pass

if is_developer:
    debug_prop_options = set()
else:
    debug_prop_options = {'HIDDEN'}

import bpy
from bpy.props import (BoolProperty,
                       FloatProperty,
                       IntProperty,
                       StringProperty,
                       CollectionProperty,
                       EnumProperty,
                       PointerProperty,
                       )
from bpy_extras.io_utils import (ImportHelper,
                                 ExportHelper,
                                 )

from PIL import Image
import pathlib
import os
module_dir = pathlib.Path(__file__).resolve().parent
palette_dir = os.path.join(module_dir,"palettes")
palette_files = [f for f in os.listdir(palette_dir) if os.path.isfile(os.path.join(palette_dir,f))]
ppl_list = []
_PALETTE_MAP = {}
for pl in palette_files:
    pal_enum_val = pathlib.Path(pl).stem.upper()
    ppl_list.append((pal_enum_val,pl,""))
    _PALETTE_MAP[pal_enum_val] = os.path.join(palette_dir,pl)
    
ppl_items = tuple(ppl_list)
print(_PALETTE_MAP)

def select_color(colors,index):
    return colors[index]

class palette_pixels:
    def __init__(self,square_size):
        self.size = square_size
        self.color = (0,0,0)
        
    def set_color(self,red,green,blue):
        self.color = (red,green,blue)


class palette_image:
    def __init__(self,fileName,color_square_size,color_count):
        self.num_colors = color_count
        self.square_size = color_square_size
        self.fileName = fileName
        self.pixSquares = []
        self.image = None
        for ii in range(color_count):
            self.pixSquares.append(palette_pixels(color_square_size))

    def set_color(self,index,rgb):
        red,green,blue = rgb
        self.pixSquares[index].set_color(red,green,blue)

    def create_image(self,num_colors_per_row):
        import math
        width = num_colors_per_row * self.square_size
        height = math.floor(self.num_colors / num_colors_per_row) * self.square_size
        img = Image.new('RGB',(width,height), "black")
        self.image = img
        pix = img.load()
        index = 0
        for pixSq in self.pixSquares:
            right = index % num_colors_per_row
            down = math.floor(index / num_colors_per_row)
            #print(f"{right} - {down}")
            for offx in range(self.square_size):
                cur_pix_x = right * self.square_size + offx
                for offy in range(self.square_size):
                    cur_pix_y = down * self.square_size + offy

                    pix[cur_pix_x,cur_pix_y] = pixSq.color
            index = index + 1
        return img

    def save(self):
        if self.image:
            self.image.save(self.fileName)

def load_palette(pal_path):
    from . import tribes_pal
    pal_data = tribes_pal.Tribes_PPL(file_name=pal_path)
    pal_data_dir = os.path.join(module_dir,"palette_data")
    #check if pal data export is needed
    pal_file_noext = pathlib.Path(pal_path).stem
    pal_data_exists = False
    for pal_id,pal_loaded in pal_data.pal.items():
        ppl = palette_image(
            os.path.join(pal_data_dir,pal_file_noext) + "%d.bmp" %(pal_loaded.index),
            1,
            256
        )
        print(f"Pal Index: {pal_loaded.index}")
        for ii in range(256):
            color = pal_data.get_color(pal_id,ii)
            ppl.set_color(ii,(color[0],color[1],color[2]))
            
        img = ppl.create_image(256)
        ppl.save()
        img.close()
            
class ImportDTS(bpy.types.Operator, ImportHelper):
    """Load a Tribes DTS File"""
    bl_idname = "import_scene.dts"
    bl_label = "Import DTS"
    bl_options = {'UNDO'} #'PRESET'

    filename_ext = ".dts"
    filter_glob : StringProperty(
        default="*.dts",
        options={'HIDDEN'}
        )

    directory: StringProperty(
        subtype='DIR_PATH',
    )

    files: CollectionProperty(
        name="File Path",
        type=bpy.types.OperatorFileListElement,
    )

    materials_flag: BoolProperty(
        name="Import Materials",
        description="Import materials and convert .bmps from selected palette",
        default=True
    )

    palette: EnumProperty(
        name="Palette",
        items=ppl_items,
        default="LUSH.DAY"
    )

    def execute(self, context):
        from . import import_dts
        import os
        paths = [os.path.join(self.directory, name.name) for name in self.files]
        #print(f"Files: {paths}")

        if not paths:
            paths.append(self.filepath)

        if bpy.ops.object.mode_set.poll():
            bpy.ops.object.mode_set(mode='OBJECT')

        if bpy.ops.object.select_all.poll():
            bpy.ops.object.select_all(action='DESELECT')

        pl = _PALETTE_MAP[self.palette]
        if self.materials_flag:
            load_palette(pl)
        import_options = import_dts.ImportParams(self.materials_flag,pl)
        for path in paths:
            import_dts.load(self, context, filepath = path, options = import_options)  #palette_prefix = pathlib.Path(pl).stem)
            
        return {'FINISHED'}

class ExportDTS(bpy.types.Operator, ExportHelper):
    """Save a Torque DTS File"""

    bl_idname = "export_scene.dts"
    bl_label = 'Export DTS'
    bl_options = {'PRESET'}

    filename_ext = ".dts"
    filter_glob : StringProperty(
        default="*.dts",
        options={'HIDDEN'},
        )

    select_object = BoolProperty(
        name="Selected objects only",
        description="Export selected objects (empties, meshes) only",
        default=False,
        )
    select_marker = BoolProperty(
        name="Selected markers only",
        description="Export selected timeline markers only, used for sequences",
        default=False,
        )

    blank_material = BoolProperty(
        name="Blank material",
        description="Add a blank material to meshes with none assigned",
        default=True,
        )

    generate_texture = EnumProperty(
        name="Generate textures",
        description="Automatically generate solid color textures for materials",
        default="disabled",
        items=(
            ("disabled", "Disabled", "Do not generate any textures"),
            ("custom-missing", "Custom (if missing)", "Generate textures for non-default material names if not already present"),
            ("custom-always", "Custom (always)", "Generate textures for non-default material names"),
            ("all-missing", "All (if missing)", "Generate textures for all materials if not already present"),
            ("all-always", "All (always)", "Generate textures for all materials"))
        )

    apply_modifiers = BoolProperty(
        name="Apply modifiers",
        description="Apply modifiers to meshes",
        default=True,
        )

    debug_report = BoolProperty(
        name="Write debug report",
        description="Dump out all the information from the DTS to a file",
        options=debug_prop_options,
        default=False,
        )

    check_extension = True

    def execute(self, context):
        from . import export_dts
        keywords = self.as_keywords(ignore=("check_existing", "filter_glob"))
        return export_dts.save(self, context, **keywords)

class SplitMeshIndex(bpy.types.Operator):
    """Split a mesh into new meshes limiting the number of indices"""

    bl_idname = "mesh.split_mesh_vindex"
    bl_label = "Split mesh by indices"
    bl_options = {"REGISTER", "UNDO"}

    def execute(self, context):
        limit = 10922

        ob = context.active_object

        if ob is None or ob.type != "MESH":
            self.report({"ERROR"}, "Select a mesh object first")
            return {"FINISHED"}

        me = ob.data

        out_me = None
        out_ob = None

        def split():
            nonlocal out_me
            nonlocal out_ob

            if out_me is not None:
                out_me.validate()
                out_me.update()

            out_me = bpy.data.meshes.new(ob.name)
            out_ob = bpy.data.objects.new(ob.name, out_me)

            context.scene.objects.link(out_ob)

            # For now, copy all verts over. See what happens?
            out_me.vertices.add(len(me.vertices))

            for vert, out_vert in zip(me.vertices, out_me.vertices):
                out_vert.co = vert.co
                out_vert.normal = vert.normal

        split()

        for poly in me.polygons:
            if poly.loop_total >= limit:
                continue

            if len(out_me.loops) + poly.loop_total > limit:
                split()

            loop_start = len(out_me.loops)
            out_me.loops.add(poly.loop_total)

            out_me.polygons.add(1)
            out_poly = out_me.polygons[-1]

            out_poly.loop_start = loop_start
            out_poly.loop_total = poly.loop_total
            out_poly.use_smooth = poly.use_smooth

            for loop_index, out_loop_index in zip(poly.loop_indices, out_poly.loop_indices):
                loop = me.loops[loop_index]
                out_loop = out_me.loops[out_loop_index]

                out_loop.normal = loop.normal
                out_loop.vertex_index = loop.vertex_index

        out_me.validate()
        out_me.update()

        return {"FINISHED"}

class HideBlockheadNodes(bpy.types.Operator):
    """Set all non-default Blockhead model apparel meshes as hidden"""

    bl_idname = "mesh.hide_blockhead_nodes"
    bl_label = "Hide Blockhead nodes on selection"
    bl_options = {"REGISTER", "UNDO"}

    blacklist = (
        "copHat",
        "knitHat",
        "pack",
        "quiver",
        "femChest",
        "epauletsRankB",
        "epauletsRankC",
        "epauletsRankD",
        "epauletsRankA",
        "skirtHip",
        "skirtTrimRight",
        "RHook",
        "RarmSlim",
        "LHook",
        "LarmSlim",
        "PointyHelmet",
        "Helmet",
        "bicorn",
        "scoutHat",
        "FlareHelmet",
        "triPlume",
        "plume",
        "septPlume",
        "tank",
        "armor",
        "cape",
        "Bucket",
        "epaulets",
        "ShoulderPads",
        "Rski",
        "Rpeg",
        "Lski",
        "Lpeg",
        "skirtTrimLeft",
        "Visor",
    )

    def execute(self, context):
        for ob in context.scene.objects:
            if ob.select and ob.type == "MESH" and ob.name in self.blacklist:
                ob.hide = True

        return {"FINISHED"}

    
class TorqueMaterialProperties(bpy.types.PropertyGroup):
    blend_mode = EnumProperty(
        name="Blend mode",
        items=(
            ("ADDITIVE", "Additive", "White is white, black is transparent"),
            ("SUBTRACTIVE", "Subtractive", "White is black, black is transparent"),
            ("NONE", "None", "I don't know how to explain this, try it yourself"),
        ),
        default="ADDITIVE")
    s_wrap = BoolProperty(name="S-Wrap", default=True)
    t_wrap = BoolProperty(name="T-Wrap", default=True)
    use_ifl = BoolProperty(name="IFL")
    ifl_name = StringProperty(name="Name")

##class DTS_import_materials(bpy.types.Panel):
##    bl_idname = "DTS_PAL_SELECT"
##    bl_label = "Pallet"
##    bl_space_type = 'FILE_BROWSER'
##    bl_region_type = 'TOOL_PROPS'
##    bl_parent_id = "FILE_PT_operator"


class TorqueMaterialPanel(bpy.types.Panel):
    bl_idname = "MATERIAL_PT_torque"
    bl_label = "Torque"
    bl_space_type = 'PROPERTIES'
    bl_region_type = 'WINDOW'
    bl_context = "material"
    bl_options = {'DEFAULT_CLOSED'}

    @classmethod
    def poll(cls, context):
        return (context.material is not None)

    def draw(self, context):
        layout = self.layout
        obj = context.material

        sublayout = layout.row()
        sublayout.enabled = obj.use_transparency
        sublayout.prop(obj.torque_props, "blend_mode", expand=True)

        row = layout.row()
        row.prop(obj.torque_props, "use_ifl")
        sublayout = row.column()
        sublayout.enabled = obj.torque_props.use_ifl
        sublayout.prop(obj.torque_props, "ifl_name", text="")
        sublayout = layout.column()
        sublayout.enabled = obj.torque_props.use_ifl

def menu_func_import_dts(self, context):
    self.layout.operator(ImportDTS.bl_idname, text="Tribes (.dts)")

def menu_func_export_dts(self, context):
    self.layout.operator(ExportDTS.bl_idname, text="Tribes (.dts)")

classes = (
    ImportDTS,
    #ExportDTS,
    TorqueMaterialProperties)

def register():
    for cls in classes:
        bpy.utils.register_class(cls)

    bpy.types.Material.torque_props = PointerProperty(
        type=TorqueMaterialProperties)

    bpy.types.TOPBAR_MT_file_import.append(menu_func_import_dts)
    #bpy.types.TOPBAR_MT_file_export.append(menu_func_export_dts)
    #bpy.types.INFO_MT_file_export.append(menu_func_export_dsq)

def unregister():
    #bpy.utils.unregister_module(__name__)

    del bpy.types.Material.torque_props

    bpy.types.TOPBAR_MT_file_import.remove(menu_func_import_dts)
    #bpy.types.TOPBAR_MT_file_export.remove(menu_func_export_dts)

    for cls in classes:
        bpy.utils.unregister_class(cls)

if __name__ == "__main__":
    register()
