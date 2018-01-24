using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AttributeWrangler
{
    public class aaObject
    {
        public string Name { get; set; }
        public bool IsTemplate { get; set; }
    }

    public enum PickerMode
    {
        List,
        Template,
        Area
    }

}
