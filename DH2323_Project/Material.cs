using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DH2323_Project
{
    internal class Material
    {
        public string Name;
        public ShaderProgram Program;

        public Texture? Albedo;
        public Texture? Normal;
        public Texture? Roughness;

        public Material(string name)
        {
            Name = name;
        }
    }
}
