from dts_module import helper

class mat_params:
    def __init__(self,data,data_index):
        self.flags = 0
        self.alpha = 0
        self.index = -1
        self.rgb = (0,0,0,0) #r,g,b,flags
        self.type = 0
        self.elasticity = 0
        self.friction = 0
        self.useDefaultProps = 0

        self.flags = helper.get_int(data,data_index)
        self.alpha = helper.get_float(data,data_index)
        self.index = helper.get_int(data,data_index)
        red = helper.get_int8(data,data_index)
        green = helper.get_int8(data,data_index)
        blue = helper.get_int8(data,data_index)
        ff = helper.get_int8(data,data_index)
        self.rgb = (red,green,blue,ff)

        self.fileName = data[data_index[0]:data_index[0]+32]
        ii = 0
        for c in self.fileName:
            print(c)
            if c == 0:
                break;
            ii += 1
        self.fileName = self.fileName[:ii].decode('utf-8')
        print(f"FileName: {self.fileName}")
        data_index[0] += 32
        
        self.type = helper.get_int(data,data_index)
        self.elasticity = helper.get_float(data,data_index)
        self.friction = helper.get_float(data,data_index)
        self.useDefaultProps = helper.get_int(data,data_index)

    def __str__(self):
        return f'Params({self.flags},{self.alpha},{self.index},{self.rgb},{self.fileName},{self.type},{self.elasticity},{self.friction},{self.useDefaultProps})'
        
class material:
    def __init__(self,data,data_index,version):
        self.version = version
        self.params = mat_params(data,data_index)

    def __str__(self):
        return f'Material({self.params})'

class material_list:
    def __init__(self,data,data_index):
        if data[data_index[0]:data_index[0] + 4] != b"PERS":
            print("Wrong PERS header")
            return

        self.matList = []
        data_index[0] += 4
        chunk_size = helper.get_int(data, data_index)
        #data_index[0] += 2 #class name size
        cns = helper.get_uint16(data,data_index)
        print(f"Class Name Size: {cns}")
        print(f"Data Index: {data_index}")
        
        if data[data_index[0]:data_index[0] + cns] != b'TS::MaterialList':
            print("Not a TS::MaterialList")
            return

        data_index[0] += cns
        version = helper.get_int(data, data_index)
        self.nDetails = helper.get_int(data,data_index)
        self.nMaterials = helper.get_int(data,data_index)

        for xx in range(self.nDetails * self.nMaterials):
            mat = material(data,data_index,version)
            self.matList.append(mat)

    def printDebug(self):
        print("Num Details: %d" %(self.nDetails))
        print("Num Materials: %d" % (self.nMaterials))
        print("Material List:")
        for mat in self.matList:
            print("\t%s" % (mat))
            
