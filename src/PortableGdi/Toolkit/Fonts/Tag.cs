using System.Linq;

namespace Portable.Drawing.Toolkit.Fonts
{
    public class Tag
    {
        private readonly int[] _data;
        public string TagString => string.Join("", _data.Select(i => (char) i));

        public Tag(params int[] data)
        {
            _data = data;
        }

        public Tag(string value)
        {
            _data = new int[4];
            for (int i = 0; i < 4; i++)
            {
                _data[i] = value[i];
            }
        }

        public override string ToString()
        {
            return TagString;
        }
    }
}