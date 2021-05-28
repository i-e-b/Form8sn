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
            if (svc == null) return value;

            var editValue = value as PropertyGridDataPicker ?? throw new Exception("Data Grid path picker was null");
            
            if (editValue.BaseProject == null) throw new Exception("Data Grid path picker had no project attached");
            var prevPath = editValue.Path;
            var pageIndex = editValue.PageDefinitionIndex;
            string[]? pagePath = null;
            if (pageIndex.HasValue) pagePath = editValue.BaseProject.Pages[pageIndex.Value].RepeatMode.DataPath;

            using var form = new PickDataSource(editValue.BaseProject, "Pick data path", prevPath, pagePath, pageIndex);
            svc.ShowDialog(form);
            if (form.SelectedPath != null) editValue.Path = form.SelectedPath;

            return editValue; // can also replace the wrapper object here
        }
    }
}