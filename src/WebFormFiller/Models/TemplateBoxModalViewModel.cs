
using System;
using System.Collections.Generic;
using System.Linq;
using Form8snCore.FileFormats;
using Form8snCore.HelpersAndConverters;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebFormFiller.Models
{
    public class TemplateBoxModalViewModel
    {
        #region Read-Only Properties
        /// <summary>
        /// DocumentId of the template project we are editing
        /// </summary>
        public int DocumentId { get; set; }
        /// <summary>
        /// Index in the document's page list that contains the box to be edited
        /// </summary>
        public int PageIndex { get; set; }
        /// <summary>
        /// Name of the box. This is used as the key into the project index file, and
        /// should NOT be edited
        /// </summary>
        public string? BoxKey { get; set; }
        
        /// <summary>
        /// Internal revision number. Used to guard against data loss if an old save command comes in very late.
        /// </summary>
        public int Version { get; set; }
        #endregion

        /// <summary>
        /// Name of the box. This will replace the old box name / key when saved.
        /// </summary>
        public string? BoxName { get; set; }

        public string? Notes { get; set; }
        
        public string? DataPath { get; set; }
        public bool WrapText { get; set; }
        public bool ShrinkToFit { get; set; }
        public string? FontSize { get; set; }
        
        /// <summary> A human-readable version of the current format </summary>
        public string? DisplayFormatDescription { get; set; }
        /// <summary> Machine-readable version of the current format </summary>
        public string? DisplayFormatJsonStruct { get; set; }
        
        public string? ProcessingOrder { get; set; }
        public string? DependsOn { get; set; }
        public IEnumerable<SelectListItem> OtherBoxKeys { get; set; } = Array.Empty<SelectListItem>();
        public TextAlignment TextAlign { get; set; }
        public bool IsRequired { get; set; }

        public static TemplateBoxModalViewModel From(TemplateProject project, int docId, int pageIndex, string boxKey)
        {
            var thePage = project.Pages[pageIndex];
            var otherBoxes = thePage.Boxes.Keys.Where(k=>k!=boxKey).Select(k=>new SelectListItem(k,k)).ToList();
            
            var theBox = thePage.Boxes[boxKey];
            
            var model = new TemplateBoxModalViewModel{
                // Keys, not to be edited
                PageIndex = pageIndex,
                Version = (project.Version ?? 0) + 1,
                DocumentId = docId,
                BoxKey = boxKey,
                OtherBoxKeys = otherBoxes,
                
                // Editable values
                BoxName = boxKey,
                Notes = theBox.Notes,
                DataPath = MappingPathToString(theBox),
                DependsOn = theBox.DependsOn,
                DisplayFormatDescription = DescribeFormat(theBox.DisplayFormat),
                DisplayFormatJsonStruct = SerialiseDisplayFormatParameters(theBox),
                
                FontSize = theBox.BoxFontSize?.ToString()??"",
                ProcessingOrder = theBox.BoxOrder?.ToString()??"",
                WrapText = theBox.WrapText,
                ShrinkToFit = theBox.ShrinkToFit,
                TextAlign = theBox.Alignment,
                IsRequired = theBox.IsRequired
            };
            return model;
        }


        /// <summary>
        /// Copy view model values into an existing index file (to perform an update)
        /// </summary>
        public void CopyTo(TemplateProject targetTemplateProject)
        {
            if (BoxKey is null) return;
            if (PageIndex < 0 || PageIndex >= targetTemplateProject.Pages.Count) return;
            
            var thePage = targetTemplateProject.Pages[PageIndex];
            var theBox = thePage.Boxes[BoxKey];
            
            // First, handle renaming of box key
            if (string.IsNullOrWhiteSpace(BoxName)) BoxName = BoxKey;
            else BoxName = Strings.CleanKeyName(BoxName);
            if (BoxKey != BoxName ) BoxKey = RenameOperations.RenamePageBox(thePage, BoxKey, BoxName);

            // Handle depends-on links
            if (string.IsNullOrWhiteSpace(DependsOn)) theBox.DependsOn = null;
            else if (EditChecks.NotCircular(thePage, DependsOn, BoxKey)) theBox.DependsOn = DependsOn;
            
            // Handle the display properties
            if (string.IsNullOrWhiteSpace(DisplayFormatJsonStruct))
                theBox.DisplayFormat = null;
            else
                theBox.DisplayFormat = SkinnyJson.Json.Defrost<DisplayFormatFilter>(DisplayFormatJsonStruct);
            
            // Now copy across the regular values
            theBox.Alignment = TextAlign;
            theBox.BoxOrder = ParseIntOrDefault(ProcessingOrder, null);
            theBox.MappingPath = string.IsNullOrWhiteSpace(DataPath) ? null : DataPath?.Split('.');
            theBox.WrapText = WrapText;
            theBox.ShrinkToFit = ShrinkToFit;
            theBox.IsRequired = IsRequired;
            theBox.BoxFontSize = ParseIntOrDefault(FontSize, theBox.BoxFontSize);
            theBox.Notes = Notes;
        }
        
        private static string DescribeFormat(DisplayFormatFilter? format)
        {
            if (format is null) return "None";
            
            var paramText = string.Join(", ",
                format.FormatParameters
                    .Where(kvp=> !string.IsNullOrEmpty(kvp.Value))
                    .Select(kvp => $"{kvp.Key} = '{kvp.Value}'")
            );
            var split = paramText.Length > 0 ? ": " : "";
            return $"{format.Type}{split}{paramText}";
        }

        private static string SerialiseDisplayFormatParameters(TemplateBox? theBox)
        {
            return theBox?.DisplayFormat is null ? "" : SkinnyJson.Json.Freeze(theBox.DisplayFormat);
        }

        private static string MappingPathToString(TemplateBox? theBox)
        {
            if (theBox?.MappingPath is null) return "";
            return string.Join(".", theBox.MappingPath);
        }

        private static int? ParseIntOrDefault(string? value, int? defaultValue) => int.TryParse(value, out var parsed) ? parsed : defaultValue;
    }
}