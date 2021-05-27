using Form8snCore.FileFormats;

namespace BasicImageFormFiller.EditForms.PropertyGridSpecialTypes
{
    public class IfElseMappingParamsUI : IfElseMappingParams
    {
        public new PropertyGridDataPicker? Same { get; set; }
        public new PropertyGridDataPicker? Different { get; set; }
    }
}