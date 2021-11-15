using System;
using System.Collections.Generic;
using System.Linq;
using Form8snCore.FileFormats;
using Form8snCore.HelpersAndConverters;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebFormFiller.Models
{
    public class FilterEditViewModel
    {
        #region Read-Only Properties
        /// <summary>
        /// DocumentId of the template project we are editing
        /// </summary>
        public int DocumentId { get; set; }
        /// <summary>
        /// Index in the document's page list that contains the filter to be edited. If null, edit document filters
        /// </summary>
        public int? PageIndex { get; set; }
        /// <summary>
        /// Name of the filter. This is used as the key into the project index file, and should NOT be edited
        /// </summary>
        public string? FilterKey { get; set; }
        
        /// <summary>
        /// Internal revision number. Used to guard against data loss if an old save command comes in very late.
        /// </summary>
        public int Version { get; set; }

        #endregion
        
        public FilterEditViewModel()
        {
            AvailableFilterTypes = EnumOptions.AllDataFilterTypes().Select(SelectorItemForEnum).ToList();
        }
        
        public static FilterEditViewModel From(IndexFile project, int? pageIndex, int docId, string filterKey, MappingInfo theFilter)
        {
            return new FilterEditViewModel{
                Version = project.Version ?? 0,
                PageIndex = pageIndex ?? -1,
                DocumentId = docId,
                FilterKey = filterKey,
                NewFilterName = filterKey,
                DataFilterType = theFilter.MappingType.ToString(),
                SourcePath = string.Join(".", theFilter.DataPath ?? Array.Empty<string>()),
                
                AllAsNumberPostfix            = ReadParam(theFilter, nameof(NumberMappingParams.Postfix)),
                AllAsNumberPrefix             = ReadParam(theFilter, nameof(NumberMappingParams.Prefix)),
                AllAsNumberDecimalPlaces      = ReadParam(theFilter, nameof(NumberMappingParams.DecimalPlaces)),
                AllAsNumberDecimalSeparator   = ReadParam(theFilter, nameof(NumberMappingParams.DecimalSeparator)),
                AllAsNumberThousandsSeparator = ReadParam(theFilter, nameof(NumberMappingParams.ThousandsSeparator)),

                ConcatInfix                   = ReadParam(theFilter, nameof(JoinMappingParams.Infix)),
                ConcatPostfix                 = ReadParam(theFilter, nameof(JoinMappingParams.Postfix)),
                ConcatPrefix                  = ReadParam(theFilter, nameof(JoinMappingParams.Prefix)),
                
                IfElseExpectedValue           = ReadParam(theFilter, nameof(IfElseMappingParams.ExpectedValue)),
                IfElseMapDifferent            = ReadParam(theFilter, nameof(IfElseMappingParams.Different)),
                IfElseMapSame                 = ReadParam(theFilter, nameof(IfElseMappingParams.Same)),
                
                FixedText                     = ReadParam(theFilter, nameof(TextMappingParams.Text)),
                
                SkipCount                     = ReadParam(theFilter, nameof(SkipMappingParams.Count)),
                
                TakeCount                     = ReadParam(theFilter, nameof(TakeMappingParams.Count)),
                
                AsDateFormatString            = ReadParam(theFilter, nameof(DateFormatMappingParams.FormatString)),
                
                SplitNMaxCount                = ReadParam(theFilter, nameof(MaxCountMappingParams.MaxCount))
            };
        }

        public void CopyTo(IndexFile project)
        {
            var filterSet = project.PickFilterSet(PageIndex);
            if (filterSet is null) throw new Exception("Filter set 'CopyTo' lost reference to filter set");
            if (string.IsNullOrWhiteSpace(FilterKey) || !filterSet.ContainsKey(FilterKey)) return;
            
            var theFilter = filterSet[FilterKey!];
            theFilter.MappingType = ParseMappingType();
            theFilter.DataPath = string.IsNullOrWhiteSpace(SourcePath) ? Array.Empty<string>() : SourcePath.Split('.');
            
            var p = new Dictionary<string, string>();
            
            TryMapTo(p, SplitNMaxCount               , nameof(MaxCountMappingParams.MaxCount));
            TryMapTo(p, AsDateFormatString           , nameof(DateFormatMappingParams.FormatString));
            TryMapTo(p, TakeCount                    , nameof(TakeMappingParams.Count));
            TryMapTo(p, SkipCount                    , nameof(SkipMappingParams.Count));
            TryMapTo(p, FixedText                    , nameof(TextMappingParams.Text));
            
            TryMapTo(p, IfElseMapSame                , nameof(IfElseMappingParams.Same));
            TryMapTo(p, IfElseMapDifferent           , nameof(IfElseMappingParams.Different));
            TryMapTo(p, IfElseExpectedValue          , nameof(IfElseMappingParams.ExpectedValue));
            
            TryMapTo(p, ConcatInfix                  , nameof(JoinMappingParams.Infix));
            TryMapTo(p, ConcatPostfix                , nameof(JoinMappingParams.Postfix));
            TryMapTo(p, ConcatPrefix                 , nameof(JoinMappingParams.Prefix));
            
            TryMapTo(p, AllAsNumberPostfix           , nameof(NumberMappingParams.Postfix));
            TryMapTo(p, AllAsNumberPrefix            , nameof(NumberMappingParams.Prefix));
            TryMapTo(p, AllAsNumberDecimalPlaces     , nameof(NumberMappingParams.DecimalPlaces));
            TryMapTo(p, AllAsNumberDecimalSeparator  , nameof(NumberMappingParams.DecimalSeparator));
            TryMapTo(p, AllAsNumberThousandsSeparator, nameof(NumberMappingParams.ThousandsSeparator));
            
            theFilter.MappingParameters = p;
            
            // If filter name has been changed, check it's valid and update
            if (!string.IsNullOrWhiteSpace(NewFilterName) && NewFilterName != FilterKey)
            {
                FilterKey = RenameOperations.RenameDataFilter(project, PageIndex, FilterKey, NewFilterName);
            }
        }

        /// <summary>
        /// UI-side selected filter type
        /// </summary>
        public string? DataFilterType { get; set; }

        public IEnumerable<SelectListItem> AvailableFilterTypes { get; set; }
        public string SourcePath { get; set; } = "";
        public string? SplitNMaxCount { get; set; }
        public string? FixedText { get; set; }
        public string? IfElseMapDifferent { get; set; }
        public string? IfElseMapSame { get; set; }
        public string? IfElseExpectedValue { get; set; }
        public string? ConcatPostfix { get; set; }
        public string? ConcatInfix { get; set; }
        public string? ConcatPrefix { get; set; }
        public string? TakeCount { get; set; }
        public string? SkipCount { get; set; }
        public string? AsDateFormatString { get; set; }
        public string? AllAsNumberPostfix { get; set; }
        public string? AllAsNumberPrefix { get; set; }
        public string? AllAsNumberDecimalSeparator { get; set; }
        public string? AllAsNumberThousandsSeparator { get; set; }
        public string? AllAsNumberDecimalPlaces { get; set; }
        public string? NewFilterName { get; set; }


        private static SelectListItem SelectorItemForEnum(EnumOption e)
        {
            if (string.IsNullOrWhiteSpace(e.Description)) return new SelectListItem(e.Name, e.Name);
            return new SelectListItem(e.Name + " - " + e.Description, e.Name);
        }

        private static string? ReadParam(MappingInfo theFilter, string name)
        {
            if (theFilter.MappingParameters.ContainsKey(name)) return theFilter.MappingParameters[name];
            return null;
        }

        private void TryMapTo(Dictionary<string,string> target, string? value, string key)
        {
            if (target.ContainsKey(key)) return;
            if (string.IsNullOrEmpty(value)) return;
            target.Add(key, value);
        }

        private MappingType ParseMappingType()
        {
            if (DataFilterType is null) return MappingType.None;
            try { return Enum.Parse<MappingType>(DataFilterType, true); }
            catch { return MappingType.None; }
        }
    }
}