using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace BasicImageFormFiller.EditForms.PropertyGridSpecialTypes
{
    class PropertyGridDataPickEditor : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            var svc = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;
            if (svc == null || !(value is PropertyGridDataPicker picker)) return value;

            if (picker.BaseProject == null) throw new Exception("Data Grid path picker had no project attached");
            var prevPath = picker.Path;
            var pageIndex = picker.PageDefinitionIndex;
            string[]? pagePath = null;
            if (pageIndex.HasValue) pagePath = picker.BaseProject.Pages[pageIndex.Value].RepeatMode.DataPath;

            using var form = new PickDataSource(picker.BaseProject, "Pick data path", prevPath, pagePath, pageIndex);
            svc.ShowDialog(form);
            if (form.SelectedPath != null) picker.Path = form.SelectedPath;

            return value; // can also replace the wrapper object here
        }
    }
}