from . import BitStream
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageColor
from . import tribes_pal


class Tribes_BMP:
    def __init__(self, bitstream = None):
        self.header = BMPHeader()
        self.stride = 0
        self.bmp_data = []
        self.image_size = 0
        self.pal_index = 0
        self.inverted = False
        if bitstream is not None:
            self.load_pbmp_bitstream(bitstream)

    def load_pbmp(self, file_name):
        with open(file_name, 'rb') as file:
            data = file.read()
            return self.load_pbmp_data(data)

    def load_pbmp_data(self, data):
        st = BitStream.BitStream(data)
        return self.load_pbmp_bitstream(st)

    def load_pbmp_bitstream(self, st: BitStream.BitStream):
        num_chunks = -1
        load_type = None
        while num_chunks != 0:
            num_chunks -= 1
            data = st.read_bytes(4)
            size = st.read_uint(32)

            if num_chunks < 0 and data[0:2] == b"BM":
                self.load_msbmp(st, 0)
                return "bmp"

            if data == b"PBMP":
                #  Do nothing <(^.^<), look at my nothing dance
                load_type = "pbmp"
                continue
            elif data == b"PiDX":
                self.pal_index = st.read_uint(32)
                continue
            elif data == b"head":
                self.header.load_bitstream(st)

                num_chunks = self.header.ver_nc & 0x00ffffff

                self.stride = ((self.header.width * self.header.bit_depth >> 3) + 3 ) & ~3
                continue
            elif data == b"RIFF":
                #This is a MS palette included...I don't feel like implementing at the moment
                st.burn(size * 8)
                continue
            elif data == b"DETL":
                detail_levels = st.read_uint(size * 8)
                continue
            elif data == b"data":
                self.image_size = size

                self.bmp_data = st.read_bytes(size)
                continue
            else:
                st.burn(size * 8)

        return load_type

    def load_msbmp(self, st: BitStream.BitStream, flags):
        #Reset back to the "beginning"
        st.bit_ptr -= 64
        header = MSBMPHeader()
        header.load_bitstream(st)

        info = MSBMPInfoHeader()
        info.load_bitstream(st)
        stride = ((info.width * info.bit_count >> 3) + 3) & (~3)
        self.header.height = info.height
        self.header.width = info.width
        self.header.bit_depth = info.bit_count
        self.stride = stride

        if header.reserved1 == 0xF5F7 and header.reserved2 != 0xffff:
            self.pal_index = header.reserved2
        else:
            self.pal_index = -1

        if flags & 1 and info.bit_count != 24:
            print("Error: palette in bitmap isn't handled yet")
            return
        elif info.bit_count != 24:
            if info.clr_used > 0:
                st.bit_ptr += info.clr_used * 32
            else:
                st.bit_ptr += 256 * 32

        image_size = info.height * stride
        self.inverted = True
        self.bmp_data = st.read_bytes(image_size)

    def save_png(self, filename, pal:tribes_pal.Tribes_Pal):
        data = np.zeros((self.header.height, self.header.width, 4), np.ubyte)

        if len(self.bmp_data) < self.header.height * self.stride:
            print("Error has occurred, not enough byte data data")
            return

        for y in range(self.header.height):
            img_y = y
            if self.inverted:
                y = self.header.height - y - 1
            for x in range(self.header.width):
                pix = self.bmp_data[x + y * self.stride]
                clr = pal.get_color(self.pal_index, pix)
                data[img_y, x, 0] = clr[0]  # If you happen to use MS Pal files to get color, 0 and 2 are switched
                data[img_y, x, 1] = clr[1]
                data[img_y, x, 2] = clr[2]
                data[img_y, x, 3] = clr[3]
        img = Image.fromarray(data)
        img.save(filename)
        return

class BMPHeader:
    def __init__(self):
        self.ver_nc = 0
        self.width = 0
        self.height = 0
        self.bit_depth = 0
        self.attribute = 0

    def load_bitstream(self, st: BitStream.BitStream):
        self.ver_nc = st.read_uint(32)
        self.width = st.read_uint(32)
        self.height = st.read_uint(32)
        self.bit_depth = st.read_uint(32)
        self.attribute = st.read_uint(32)

class MSBMPHeader:
    def __init__(self):
        self.type = 0
        self.size = 0
        self.reserved1 = 0
        self.reserved2 = 0
        self.offbits = 0

    def load_bitstream(self, st: BitStream.BitStream):
        self.type = st.read_uint(16)
        self.size = st.read_uint(32)
        self.reserved1 = st.read_uint(16)
        self.reserved2 = st.read_uint(16)
        self.offbits = st.read_uint(32)

class MSBMPInfoHeader:
    def __init__(self):
        self.size = 0
        self.width = 0
        self.height = 0
        self.planes = 0
        self.bit_count = 0
        self.compression = 0
        self.image_size = 0
        self.x_pels_per_meter = 0
        self.y_pels_per_meter = 0
        self.clr_used = 0
        self.clr_important = 0

    def load_bitstream(self, st: BitStream.BitStream):
        self.size = st.read_uint(32)
        self.width = st.read_uint(32)
        self.height = st.read_uint(32)
        self.planes = st.read_uint(16)
        self.bit_count = st.read_uint(16)
        self.compression = st.read_uint(32)
        self.image_size = st.read_uint(32)
        self.x_pels_per_meter = st.read_uint(32)
        self.y_pels_per_meter = st.read_uint(32)
        self.clr_used = st.read_uint(32)
        self.clr_important = st.read_uint(32)
