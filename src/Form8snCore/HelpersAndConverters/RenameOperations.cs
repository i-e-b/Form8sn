using System.Collections.Generic;
using Form8snCore.FileFormats;

namespace Form8snCore.HelpersAndConverters
{
    /// <summary>
    /// Helpers to rename box and filter keys without breaking references
    /// </summary>
    public class RenameOperations
    {
        public static string RenamePageBox(TemplatePage page, string oldKey, string? newKey)
        {
            if (!EditChecks.IsValidBoxRename(page, oldKey, newKey)) return oldKey;
            var theBox = page.Boxes[oldKey];
            var safeName = Strings.CleanKeyName(newKey);

            // Rename the box, fix existing 'depends-on' references
            page.Boxes.Remove(oldKey);
            page.FixReferences(oldKey, safeName!);
            page.Boxes.Add(safeName!, theBox);
            return safeName!;
        }

        public static string RenameDataFilter(TemplateProject project, int? pageIdx, string oldKey, string? newKey)
        {
            var filterSet = project.PickFilterSet(pageIdx);
            if (filterSet is null || !filterSet.ContainsKey(oldKey)) return oldKey;
            var value = filterSet[oldKey];

            if (!EditChecks.IsValidFilterRename(project, pageIdx, oldKey, newKey)) return oldKey;
            var safeName = Strings.CleanKeyName(newKey);
            if (string.IsNullOrWhiteSpace(safeName!)) return oldKey;

            // Rename the filter
            filterSet.Add(safeName, value);
            filterSet.Remove(oldKey);

            // Fix any existing data path pairs of {'#',oldKey}
            // These could be in box data paths, or the paths of other filters (including params of IfElse)
            if (pageIdx is null || pageIdx < 0) // rename in document filter set, and every page
            {
                RenameFilterInFilterSet(project.DataFilters, oldKey, safeName);
                foreach (var page in project.Pages)
                {
                    if (page.PageDataFilters.ContainsKey(oldKey)) continue; // over-ridden, so don't rename
                    RenameFilterInFilterSet(page.PageDataFilters, oldKey, safeName);
                    RenameFilterInBoxes(page.Boxes, oldKey, safeName);
                }
            }
            else // rename only in a single page
            {
                if (pageIdx.Value <= project.Pages.Count)
                    RenameFilterInBoxes(project.Pages[pageIdx.Value]?.Boxes, oldKey, safeName);
                RenameFilterInFilterSet(filterSet, oldKey, safeName);
            }

            return safeName!;
        }

        private static void RenameFilterInFilterSet(Dictionary<string, MappingInfo>? filterSet, string oldKey, string newKey)
        {
            const string ifElseKey1 = nameof(IfElseMappingParams.Different);
            const string ifElseKey2 = nameof(IfElseMappingParams.Same);

            var oldPrefix = Strings.FilterMarker + Strings.Separator + oldKey;
            var newPrefix = Strings.FilterMarker + Strings.Separator + newKey;

            if (filterSet is null) return;
            foreach (var filter in filterSet.Values)
            {
                if (filter.DataPath is null) continue;
                if (filter.DataPath.Length < 2) continue;

                if (filter.DataPath[0] == "#" && filter.DataPath[1] == oldKey)
                {
                    filter.DataPath[1] = newKey;
                }

                TryRewriteParam(filter, ifElseKey1, oldPrefix, newPrefix);
                TryRewriteParam(filter, ifElseKey2, oldPrefix, newPrefix);
            }
        }

        private static void TryRewriteParam(MappingInfo? filter, string paramName, string? oldPrefix, string? newPrefix)
        {
            if (filter is null || oldPrefix is null || newPrefix is null) return;
            if (filter.MappingParameters.ContainsKey(paramName)
                && filter.MappingParameters[paramName]?.StartsWith(oldPrefix) == true)
            {
                filter.MappingParameters[paramName] = filter.MappingParameters[paramName]!.Replace(oldPrefix, newPrefix);
            }
        }

        private static void RenameFilterInBoxes(IDictionary<string, TemplateBox>? boxes, string oldKey, string newKey)
        {
            if (boxes is null) return;
            foreach (var box in boxes.Values)
            {
                if (box.MappingPath is null) continue;
                if (box.MappingPath.Length < 2) continue;

                if (box.MappingPath[0] == "#" && box.MappingPath[1] == oldKey)
                {
                    box.MappingPath[1] = newKey;
                }
            }
        }
    }
}