
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
        /// Url for the client to load the data-picker partial view into a modal
        /// </summary>
        public string? LoadDataPickerUrl { get; set; }
        #endregion

        /// <summary>
        /// Internal revision number. Used to guard against data loss if an old save command comes in very late.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Name of the box. This will replace the old box name / key when saved.
        /// </summary>
        public string? BoxName { get; set; }
        
        public string? DataPath { get; set; }
        public bool WrapText { get; set; }
        public bool ShrinkToFit { get; set; }
        public string? FontSize { get; set; }
        
        public string? DisplayFormatDescription { get; set; }
        public DisplayFormatFilter? DisplayFormat { get; set; }
        public string? ProcessingOrder { get; set; }
        public string? DependsOn { get; set; }
        public IEnumerable<SelectListItem> OtherBoxKeys { get; set; } = Array.Empty<SelectListItem>();
        public TextAlignment TextAlign { get; set; }

        public static TemplateBoxModalViewModel From(IndexFile project, int documentTemplateId, int pageIndex, string boxKey)
        {
            var thePage = project.Pages[pageIndex];
            var otherBoxes = thePage.Boxes.Keys.Where(k=>k!=boxKey).Select(k=>new SelectListItem(k,k)).ToList();
            
            var theBox = thePage.Boxes[boxKey];
            
            var model = new TemplateBoxModalViewModel{
                // Keys, not to be edited
                PageIndex = pageIndex,
                DocumentId = documentTemplateId,
                BoxKey = boxKey,
                OtherBoxKeys = otherBoxes,
                
                // Editable values
                BoxName = boxKey,
                DataPath = MappingPathToString(theBox),
                DependsOn = theBox.DependsOn,
                DisplayFormat = theBox.DisplayFormat,
                DisplayFormatDescription = DescribeFormat(theBox.DisplayFormat),
                FontSize = theBox.BoxFontSize?.ToString()??"",
                ProcessingOrder = theBox.BoxOrder?.ToString()??"",
                WrapText = theBox.WrapText,
                ShrinkToFit = theBox.ShrinkToFit,
                TextAlign = theBox.Alignment
            };
            return model;
        }

        private static string DescribeFormat(DisplayFormatFilter? format)
        {
            if (format is null) return "None";
            
            var paramText = string.Join(", ",
                format.FormatParameters
                    .Where(kvp=> !string.IsNullOrEmpty(kvp.Value))
                    .Select(kvp => $"{kvp.Key} = '{kvp.Value}'")
            );
            var split = paramText.Length > 0 ? ":\r\n" : "";
            return $"{format.Type}{split}{paramText}";
        }

        private static string MappingPathToString(TemplateBox? theBox)
        {
            if (theBox?.MappingPath is null) return "";
            return string.Join(".", theBox.MappingPath);
        }

        /// <summary>
        /// Copy view model values into an existing index file (to perform an update)
        /// </summary>
        public void CopyTo(IndexFile targetIndexFile)
        {
            if (BoxKey is null) return;
            if (PageIndex < 0 || PageIndex >= targetIndexFile.Pages.Count) return;
            
            var thePage = targetIndexFile.Pages[PageIndex];
            var theBox = thePage.Boxes[BoxKey];
            
            // First, handle renaming of box key
            if (string.IsNullOrWhiteSpace(BoxName)) BoxName = BoxKey;
            else BoxName = Strings.CleanKeyName(BoxName);
            
            if (EditChecks.IsValidRename(thePage, BoxKey, BoxName))
            {
                // Rename the box, fix existing 'depends-on' references
                thePage.Boxes.Remove(BoxKey);
                thePage.FixReferences(BoxKey, BoxName!);
                thePage.Boxes.Add(BoxName!, theBox);
                BoxKey = BoxName;
            }
            if (BoxKey is null) throw new Exception("Logic error");

            // Handle depends-on links
            if (string.IsNullOrWhiteSpace(DependsOn)) theBox.DependsOn = null;
            else if (EditChecks.NotCircular(thePage, DependsOn, BoxKey)) theBox.DependsOn = DependsOn;
            
            // TODO: handle the display properties
            //theBox.DisplayFormat
            
            // Now copy across the regular values
            theBox.Alignment = TextAlign;
            theBox.BoxOrder = ParseIntOrDefault(ProcessingOrder, theBox.BoxOrder);
            theBox.MappingPath = string.IsNullOrWhiteSpace(DataPath) ? null : DataPath?.Split('.');
            theBox.WrapText = WrapText;
            theBox.ShrinkToFit = ShrinkToFit;
            theBox.BoxFontSize = ParseIntOrDefault(FontSize, theBox.BoxFontSize);
        }

        private static int? ParseIntOrDefault(string? value, int? defaultValue) => int.TryParse(value, out var parsed) ? parsed : defaultValue;
    }
}