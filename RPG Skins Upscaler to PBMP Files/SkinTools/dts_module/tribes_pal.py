import numpy as np

from . import BitStream


# Incomplete, but needed the basics for convertion to PBMP to png and I happen to have pal versions of ppl files

def pal_to_list(file):
    with open(file, "rb") as file:
        data = file.read()
        result = np.frombuffer(data, dtype=np.ubyte, offset=24, count=256*4)
        result = result.reshape((256, 4))

        return result
    return None


class Tribes_PPL:
    def __init__(self, st: BitStream.BitStream = None, file_name = None):
        self.pal = {}
        self.num_palettes = 0
        self.shade_shift = 0
        self.haze_levels = 0
        self.haze_color = 0
        self.shade_levels = 0
        self.allowed_color_matches = 0

        if st is not None:
            self.load_bitstream(st)
        if file_name is not None:
            self.load_file(file_name)

    def load_file(self, file_name):
        with open(file_name, "rb") as file:
            st = BitStream.BitStream(file.read())
            return self.load_bitstream(st)

    def load_bitstream(self, st: BitStream.BitStream):
        data = st.read_bytes(4)
        if data == b"RIFF":
            # MS palette, currently not handled
            print("Bov was too lazy to implement internal MS pal.  Contact him if you see this with the file as an example")
            return False

        if data != b"PL98":
            print("Not a ppl file")
            return False

        self.num_palettes = st.read_uint(32)
        self.shade_shift = st.read_uint(32)
        self.haze_levels = st.read_uint(32)
        self.haze_color = st.read_uint(32)
        self.allowed_color_matches = st.read_bytes(32)

        self.shade_levels = 1 << self.shade_shift

        for _ in range(self.num_palettes):
            pal = Tribes_Pal(st)
            self.pal[pal.index] = pal

    def get_color(self, pal_id, index):
        clr = self.pal[pal_id].clr_array[index]
        return clr

class Tribes_Pal:
    def __init__(self, st: BitStream.BitStream = None):
        self.clr_array = 0

        self.index = 0
        self.type = 0

        if st is not None:
            self.load_bitstream(st)

    def load_bitstream(self, st: BitStream.BitStream):
        self.clr_array = np.frombuffer(st.read_bytes(1024), dtype=np.ubyte, offset=0, count=256*4)
        self.clr_array = self.clr_array.reshape((256, 4))

        self.index = st.read_uint(32)
        self.type = st.read_uint(32)
