using Tag;

namespace BasicImageFormFiller.Interfaces
{
    public interface ITagModuleScreen
    {
        public void ShowPage(TagContent bodyContent);
        public void ShowNewTemplate();
        public void ShowLoadTemplate();
        public void ShowFailure(string message);
        void SwitchToModule(IScreenModule nextModule);
        
        /// <summary> Pick an existing file </summary>
        bool PickAFile(out string? path);
        
        /// <summary> Ask user to create a new file </summary>
        string? PickNewFile();
    }
}