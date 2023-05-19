using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DH2323_Project
{
    internal struct DDSData
    {
        public DDSFormat Format;
        public int Width;
        public int Height;

        public byte[] Data;
    }

    internal enum DDSFormat
    {
        // FIXME: fill in this.
        RGBA32F,
        RG32F,
    }

    internal static unsafe class DDSReader
    {
        struct DDS_PIXELFORMAT
        {
            public uint dwSize;
            public DDPF dwFlags;
            public FourCC dwFourCC;
            public uint dwRGBBitCount;
            public uint dwRBitMask;
            public uint dwGBitMask;
            public uint dwBBitMask;
            public uint dwABitMask;
        };

        struct DDS_HEADER
        {
            public uint dwSize;
            public DDSD dwFlags;
            public uint dwHeight;
            public uint dwWidth;
            public uint dwPitchOrLinearSize;
            public uint dwDepth;
            public uint dwMipMapCount;
            public fixed uint dwReserved1[11];
            public DDS_PIXELFORMAT ddspf;
            public DDSCAPS dwCaps;
            public DDSCAPS2 dwCaps2;
            public uint dwCaps3;
            public uint dwCaps4;
            public uint dwReserved2;
        }

        struct DDS_HEADER_DXT10
        {
            public DXGI_FORMAT dxgiFormat;
            public D3D10_RESOURCE_DIMENSION resourceDimension;
            public DDS_RESOURCE_MISC miscFlag;
            public uint arraySize;
            public DDS_RESOURCE_MISC2 miscFlags2;
        };

        public static DDSData ReadDDSFile(string path)
        {
            byte[] file = File.ReadAllBytes(path);
            fixed(byte* ptr = file)
            {
                byte* filePtr = ptr;
                // FIXME: Endianess!
                if (((int*)filePtr)[0] != 0x20534444)
                {
                    throw new FormatException("Not a DDS file!");
                }
                filePtr += 4;

                DDS_HEADER* header = (DDS_HEADER*)filePtr;

                bool fourCC = header->ddspf.dwFlags.HasFlag(DDPF.DDPF_FOURCC);

                if (fourCC == false)
                {
                    throw new NotSupportedException("We only support FourCC DDS files.");
                }

                if (header->ddspf.dwFourCC != FourCC.DX10)
                {
                    throw new NotSupportedException($"We only support DX10 format textures. Got: {header->ddspf.dwFourCC}");
                }

                DDS_HEADER_DXT10* dx10 = (DDS_HEADER_DXT10*)(filePtr + sizeof(DDS_HEADER));

                DDSFormat format;
                switch (dx10->dxgiFormat)
                {
                    case DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_FLOAT:
                        format = DDSFormat.RGBA32F; 
                        break;
                    case DXGI_FORMAT.DXGI_FORMAT_R32G32_FLOAT:
                        format = DDSFormat.RG32F;
                        break;
                    default:
                        throw new NotSupportedException($"We only support RGBA32F and RG32F for now. Got: {dx10->dxgiFormat}");
                }

                int offset = 4 + sizeof(DDS_HEADER) + sizeof(DDS_HEADER_DXT10);

                int size = file.Length - offset;
                
                return new DDSData()
                {
                    Format = format,
                    Width = (int)header->dwWidth,
                    Height = (int)header->dwHeight,
                    Data = file[offset..(offset + size)],
                };

                Console.WriteLine($"{path}");
                Console.WriteLine($"Flags: {header->dwFlags}");
                Console.WriteLine($"Height: {header->dwHeight}");
                Console.WriteLine($"Width: {header->dwWidth}");
                Console.WriteLine($"Pitch or Linear Size: {header->dwPitchOrLinearSize}");
                Console.WriteLine($"Depth: {header->dwDepth}");
                Console.WriteLine($"MipMap Count: {header->dwMipMapCount}");
                
                Console.WriteLine("Pixel Format: ");
                Console.WriteLine($"  Flags: {header->ddspf.dwFlags}");
                // FIXME: Write as chars.
                Console.WriteLine($"  FourCC: {header->ddspf.dwFourCC}");
                Console.WriteLine($"  RGB Bit Count: {header->ddspf.dwRGBBitCount}");
                Console.WriteLine($"  Red Mask: {header->ddspf.dwRBitMask}");
                Console.WriteLine($"  Green Mask: {header->ddspf.dwGBitMask}");
                Console.WriteLine($"  Blue Mask: {header->ddspf.dwBBitMask}");
                Console.WriteLine($"  Alpha Mask: {header->ddspf.dwABitMask}");

                Console.WriteLine($"Caps: {header->dwCaps}");
                Console.WriteLine($"Caps2: {header->dwCaps2}");

                Console.WriteLine($"DX10 Header:");
                Console.WriteLine($"  Format: {dx10->dxgiFormat}");
                Console.WriteLine($"  Dimension: {dx10->resourceDimension}");
                Console.WriteLine($"  Misc Flag: {dx10->miscFlag}");
                Console.WriteLine($"  Array Size: {dx10->arraySize}");
                Console.WriteLine($"  Misc Flags 2: {dx10->miscFlags2}");
            }
        }

        [Flags]
        enum DDSD : uint
        {
            /// <summary>
            /// Required in every .dds file.
            /// </summary>
            DDSD_CAPS = 0x1,
            /// <summary>
            /// Required in every .dds file.
            /// </summary>
            DDSD_HEIGHT = 0x2,
            /// <summary>
            /// Required in every .dds file.
            /// </summary>
            DDSD_WIDTH = 0x4,
            /// <summary>
            /// Required when pitch is provided for an uncompressed texture.
            /// </summary>
            DDSD_PITCH = 0x8,
            /// <summary>
            /// Required in every .dds file.
            /// </summary>
            DDSD_PIXELFORMAT = 0x1000,
            /// <summary>
            /// Required in a mipmapped texture.
            /// </summary>
            DDSD_MIPMAPCOUNT = 0x20000,
            /// <summary>
            /// Required when pitch is provided for a compressed texture.
            /// </summary>
            DDSD_LINEARSIZE = 0x80000,
            /// <summary>
            /// Required in a depth texture.
            /// </summary>
            DDSD_DEPTH = 0x800000,
        }

        [Flags]
        enum DDPF : uint
        {
            /// <summary>
            /// Texture contains alpha data; dwRGBAlphaBitMask contains valid data.
            /// </summary>
            DDPF_ALPHAPIXELS = 0x1,
            /// <summary>
            /// Used in some older DDS files for alpha channel only uncompressed data (dwRGBBitCount contains the alpha channel bitcount; dwABitMask contains valid data)
            /// </summary>
            DDPF_ALPHA = 0x2,
            /// <summary>
            /// Texture contains compressed RGB data; dwFourCC contains valid data.
            /// </summary>
            DDPF_FOURCC = 0x4,
            /// <summary>
            /// Texture contains uncompressed RGB data; dwRGBBitCount and the RGB masks(dwRBitMask, dwGBitMask, dwBBitMask) contain valid data.
            /// </summary>
            DDPF_RGB = 0x40,
            /// <summary>
            /// Used in some older DDS files for YUV uncompressed data(dwRGBBitCount contains the YUV bit count; dwRBitMask contains the Y mask, dwGBitMask contains the U mask, dwBBitMask contains the V mask)
            /// </summary>
            DDPF_YUV = 0x200,
            /// <summary>
            /// Used in some older DDS files for single channel color uncompressed data(dwRGBBitCount contains the luminance channel bit count; dwRBitMask contains the channel mask). Can be combined with DDPF_ALPHAPIXELS for a two channel DDS file.
            /// </summary>
            DDPF_LUMINANCE = 0x20000
        }

        enum FourCC : uint
        {
            DXT1 = 'D'<<0|'X'<<8|'T'<<16|'1'<<24,
            DXT2 = 'D'<<0|'X'<<8|'T'<<16|'2'<<24,
            DXT3 = 'D'<<0|'X'<<8|'T'<<16|'3'<<24,
            DXT4 = 'D'<<0|'X'<<8|'T'<<16|'4'<<24,
            DXT5 = 'D'<<0|'X'<<8|'T'<<16|'5'<<24,
            DX10 = 'D'<<0|'X'<<8|'1'<<16|'0'<<24,
        }

        [Flags]
        enum DDSCAPS : uint
        {
            /// <summary>
            /// Optional; must be used on any file that contains more than one surface (a mipmap, a cubic environment map, or mipmapped volume texture).
            /// </summary>
            DDSCAPS_COMPLEX = 0x8,

            /// <summary>
            /// Optional; should be used for a mipmap.
            /// </summary>
            DDSCAPS_MIPMAP = 0x400000,

            /// <summary>
            /// Required.
            /// </summary>
            DDSCAPS_TEXTURE = 0x1000,
        }

        enum DDSCAPS2 : uint
        {
            /// <summary>
            /// Required for a cube map.
            /// </summary>
            DDSCAPS2_CUBEMAP = 0x200,
            /// <summary>
            /// Required when these surfaces are stored in a cube map.
            /// </summary>
            DDSCAPS2_CUBEMAP_POSITIVEX = 0x400,
            /// <summary>
            /// Required when these surfaces are stored in a cube map.
            /// </summary>
            DDSCAPS2_CUBEMAP_NEGATIVEX = 0x800,
            /// <summary>
            /// Required when these surfaces are stored in a cube map.
            /// </summary>
            DDSCAPS2_CUBEMAP_POSITIVEY = 0x1000,
            /// <summary>
            /// Required when these surfaces are stored in a cube map.
            /// </summary>
            DDSCAPS2_CUBEMAP_NEGATIVEY = 0x2000,
            /// <summary>
            /// Required when these surfaces are stored in a cube map.
            /// </summary>
            DDSCAPS2_CUBEMAP_POSITIVEZ = 0x4000,
            /// <summary>
            /// Required when these surfaces are stored in a cube map.
            /// </summary>
            DDSCAPS2_CUBEMAP_NEGATIVEZ = 0x8000,
            /// <summary>
            /// Required for a volume texture.
            /// </summary>
            DDSCAPS2_VOLUME = 0x200000,
        }

        enum D3D10_RESOURCE_DIMENSION
        {
            D3D10_RESOURCE_DIMENSION_UNKNOWN = 0,
            D3D10_RESOURCE_DIMENSION_BUFFER = 1,
            D3D10_RESOURCE_DIMENSION_TEXTURE1D = 2,
            D3D10_RESOURCE_DIMENSION_TEXTURE2D = 3,
            D3D10_RESOURCE_DIMENSION_TEXTURE3D = 4
        }

        enum DXGI_FORMAT
        {
            DXGI_FORMAT_UNKNOWN = 0,
            DXGI_FORMAT_R32G32B32A32_TYPELESS = 1,
            DXGI_FORMAT_R32G32B32A32_FLOAT = 2,
            DXGI_FORMAT_R32G32B32A32_UINT = 3,
            DXGI_FORMAT_R32G32B32A32_SINT = 4,
            DXGI_FORMAT_R32G32B32_TYPELESS = 5,
            DXGI_FORMAT_R32G32B32_FLOAT = 6,
            DXGI_FORMAT_R32G32B32_UINT = 7,
            DXGI_FORMAT_R32G32B32_SINT = 8,
            DXGI_FORMAT_R16G16B16A16_TYPELESS = 9,
            DXGI_FORMAT_R16G16B16A16_FLOAT = 10,
            DXGI_FORMAT_R16G16B16A16_UNORM = 11,
            DXGI_FORMAT_R16G16B16A16_UINT = 12,
            DXGI_FORMAT_R16G16B16A16_SNORM = 13,
            DXGI_FORMAT_R16G16B16A16_SINT = 14,
            DXGI_FORMAT_R32G32_TYPELESS = 15,
            DXGI_FORMAT_R32G32_FLOAT = 16,
            DXGI_FORMAT_R32G32_UINT = 17,
            DXGI_FORMAT_R32G32_SINT = 18,
            DXGI_FORMAT_R32G8X24_TYPELESS = 19,
            DXGI_FORMAT_D32_FLOAT_S8X24_UINT = 20,
            DXGI_FORMAT_R32_FLOAT_X8X24_TYPELESS = 21,
            DXGI_FORMAT_X32_TYPELESS_G8X24_UINT = 22,
            DXGI_FORMAT_R10G10B10A2_TYPELESS = 23,
            DXGI_FORMAT_R10G10B10A2_UNORM = 24,
            DXGI_FORMAT_R10G10B10A2_UINT = 25,
            DXGI_FORMAT_R11G11B10_FLOAT = 26,
            DXGI_FORMAT_R8G8B8A8_TYPELESS = 27,
            DXGI_FORMAT_R8G8B8A8_UNORM = 28,
            DXGI_FORMAT_R8G8B8A8_UNORM_SRGB = 29,
            DXGI_FORMAT_R8G8B8A8_UINT = 30,
            DXGI_FORMAT_R8G8B8A8_SNORM = 31,
            DXGI_FORMAT_R8G8B8A8_SINT = 32,
            DXGI_FORMAT_R16G16_TYPELESS = 33,
            DXGI_FORMAT_R16G16_FLOAT = 34,
            DXGI_FORMAT_R16G16_UNORM = 35,
            DXGI_FORMAT_R16G16_UINT = 36,
            DXGI_FORMAT_R16G16_SNORM = 37,
            DXGI_FORMAT_R16G16_SINT = 38,
            DXGI_FORMAT_R32_TYPELESS = 39,
            DXGI_FORMAT_D32_FLOAT = 40,
            DXGI_FORMAT_R32_FLOAT = 41,
            DXGI_FORMAT_R32_UINT = 42,
            DXGI_FORMAT_R32_SINT = 43,
            DXGI_FORMAT_R24G8_TYPELESS = 44,
            DXGI_FORMAT_D24_UNORM_S8_UINT = 45,
            DXGI_FORMAT_R24_UNORM_X8_TYPELESS = 46,
            DXGI_FORMAT_X24_TYPELESS_G8_UINT = 47,
            DXGI_FORMAT_R8G8_TYPELESS = 48,
            DXGI_FORMAT_R8G8_UNORM = 49,
            DXGI_FORMAT_R8G8_UINT = 50,
            DXGI_FORMAT_R8G8_SNORM = 51,
            DXGI_FORMAT_R8G8_SINT = 52,
            DXGI_FORMAT_R16_TYPELESS = 53,
            DXGI_FORMAT_R16_FLOAT = 54,
            DXGI_FORMAT_D16_UNORM = 55,
            DXGI_FORMAT_R16_UNORM = 56,
            DXGI_FORMAT_R16_UINT = 57,
            DXGI_FORMAT_R16_SNORM = 58,
            DXGI_FORMAT_R16_SINT = 59,
            DXGI_FORMAT_R8_TYPELESS = 60,
            DXGI_FORMAT_R8_UNORM = 61,
            DXGI_FORMAT_R8_UINT = 62,
            DXGI_FORMAT_R8_SNORM = 63,
            DXGI_FORMAT_R8_SINT = 64,
            DXGI_FORMAT_A8_UNORM = 65,
            DXGI_FORMAT_R1_UNORM = 66,
            DXGI_FORMAT_R9G9B9E5_SHAREDEXP = 67,
            DXGI_FORMAT_R8G8_B8G8_UNORM = 68,
            DXGI_FORMAT_G8R8_G8B8_UNORM = 69,
            DXGI_FORMAT_BC1_TYPELESS = 70,
            DXGI_FORMAT_BC1_UNORM = 71,
            DXGI_FORMAT_BC1_UNORM_SRGB = 72,
            DXGI_FORMAT_BC2_TYPELESS = 73,
            DXGI_FORMAT_BC2_UNORM = 74,
            DXGI_FORMAT_BC2_UNORM_SRGB = 75,
            DXGI_FORMAT_BC3_TYPELESS = 76,
            DXGI_FORMAT_BC3_UNORM = 77,
            DXGI_FORMAT_BC3_UNORM_SRGB = 78,
            DXGI_FORMAT_BC4_TYPELESS = 79,
            DXGI_FORMAT_BC4_UNORM = 80,
            DXGI_FORMAT_BC4_SNORM = 81,
            DXGI_FORMAT_BC5_TYPELESS = 82,
            DXGI_FORMAT_BC5_UNORM = 83,
            DXGI_FORMAT_BC5_SNORM = 84,
            DXGI_FORMAT_B5G6R5_UNORM = 85,
            DXGI_FORMAT_B5G5R5A1_UNORM = 86,
            DXGI_FORMAT_B8G8R8A8_UNORM = 87,
            DXGI_FORMAT_B8G8R8X8_UNORM = 88,
            DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM = 89,
            DXGI_FORMAT_B8G8R8A8_TYPELESS = 90,
            DXGI_FORMAT_B8G8R8A8_UNORM_SRGB = 91,
            DXGI_FORMAT_B8G8R8X8_TYPELESS = 92,
            DXGI_FORMAT_B8G8R8X8_UNORM_SRGB = 93,
            DXGI_FORMAT_BC6H_TYPELESS = 94,
            DXGI_FORMAT_BC6H_UF16 = 95,
            DXGI_FORMAT_BC6H_SF16 = 96,
            DXGI_FORMAT_BC7_TYPELESS = 97,
            DXGI_FORMAT_BC7_UNORM = 98,
            DXGI_FORMAT_BC7_UNORM_SRGB = 99,
            DXGI_FORMAT_AYUV = 100,
            DXGI_FORMAT_Y410 = 101,
            DXGI_FORMAT_Y416 = 102,
            DXGI_FORMAT_NV12 = 103,
            DXGI_FORMAT_P010 = 104,
            DXGI_FORMAT_P016 = 105,
            DXGI_FORMAT_420_OPAQUE = 106,
            DXGI_FORMAT_YUY2 = 107,
            DXGI_FORMAT_Y210 = 108,
            DXGI_FORMAT_Y216 = 109,
            DXGI_FORMAT_NV11 = 110,
            DXGI_FORMAT_AI44 = 111,
            DXGI_FORMAT_IA44 = 112,
            DXGI_FORMAT_P8 = 113,
            DXGI_FORMAT_A8P8 = 114,
            DXGI_FORMAT_B4G4R4A4_UNORM = 115,
            DXGI_FORMAT_P208 = 130,
            DXGI_FORMAT_V208 = 131,
            DXGI_FORMAT_V408 = 132,
            DXGI_FORMAT_SAMPLER_FEEDBACK_MIN_MIP_OPAQUE,
            DXGI_FORMAT_SAMPLER_FEEDBACK_MIP_REGION_USED_OPAQUE,
            DXGI_FORMAT_FORCE_UINT = unchecked((int)0xffffffff)
        }

        enum DDS_RESOURCE_MISC : uint
        {
            /// <summary>
            /// Indicates a 2D texture is a cube-map texture.
            /// </summary>
            DDS_RESOURCE_MISC_TEXTURECUBE = 0x4,
        }

        enum DDS_RESOURCE_MISC2 : uint
        {
            /// <summary>
            /// Alpha channel content is unknown. This is the value for legacy files, which typically is assumed to be 'straight' alpha.
            /// </summary>
            DDS_ALPHA_MODE_UNKNOWN = 0x0,
            /// <summary>
            /// Any alpha channel content is presumed to use straight alpha.
            /// </summary>
            DDS_ALPHA_MODE_STRAIGHT = 0x1,
            /// <summary>
            /// Any alpha channel content is using premultiplied alpha. The only legacy file formats that indicate this information are 'DX2' and 'DX4'.
            /// </summary>
            DDS_ALPHA_MODE_PREMULTIPLIED = 0x2,
            /// <summary>
            /// Any alpha channel content is all set to fully opaque.
            /// </summary>
            DDS_ALPHA_MODE_OPAQUE = 0x3,
            /// <summary>
            /// Any alpha channel content is being used as a 4th channel and is not intended to represent transparency (straight or premultiplied).
            /// </summary>
            DDS_ALPHA_MODE_CUSTOM = 0x4,
        }
    }
}
