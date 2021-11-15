using Form8snCore.FileFormats;

namespace Form8snCore.HelpersAndConverters
{
    /// <summary>
    /// Safety and sanity checks for incoming edits
    /// </summary>
    public class EditChecks
    {
        /// <summary>
        /// Returns true if the old and new keys are different, and the new key is valid as part of a rename
        /// </summary>
        public static bool IsValidBoxRename(TemplatePage page, string oldKey, string? newKey)
        {
            if (string.IsNullOrWhiteSpace(newKey!)) return false; // new name is junk
            if (page.Boxes.ContainsKey(newKey)) return false; // name is already in use
            
            return newKey != oldKey;
        }
        
        /// <summary>
        /// Returns true if the old and new keys are difference, and the the keys is valid as part of a rename.
        /// If the pageIdx is null, this checks for document-wide filters
        /// </summary>
        public static bool IsValidFilterRename(IndexFile project, int? pageIdx, string oldKey, string? newKey)
        {
            var filterSet = project.PickFilterSet(pageIdx);
            if (filterSet is null) return false;
            if (string.IsNullOrWhiteSpace(newKey!)) return false; // new name is junk
            if (filterSet.ContainsKey(newKey)) return false; // name is already in use
            
            return newKey != oldKey;
        }

        /// <summary>
        /// Returns true if the given box can depend on another without causing a circular reference.
        /// This doesn't care if all references are correct or not.
        /// </summary>
        /// <param name="page">Page that the boxes are displayed on</param>
        /// <param name="boxKey">Box we are editing</param>
        /// <param name="boxDependsOn">The box key we want to depend on</param>
        public static bool NotCircular(TemplatePage? page, string boxDependsOn, string boxKey)
        {
            const bool circular = false;
            const bool ok = true;
            
            if (page == null) return circular;
            if (boxDependsOn == boxKey) return circular;
            
            var currentBox = page.Boxes[boxDependsOn];
            for (int i = 0; i < 255; i++) // safety limit
            {
                if (currentBox?.DependsOn == null) return ok;
                if (currentBox.DependsOn == boxKey) return circular;
                
                if (!page.Boxes.ContainsKey(currentBox.DependsOn)) return ok; // the chain is broken, but no circular
                currentBox = page.Boxes[currentBox.DependsOn!];
            }
            return circular;
        }
    }
}