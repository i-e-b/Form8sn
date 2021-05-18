using Tag;

namespace BasicImageFormFiller
{
    public interface ITagModuleScreen
    {
        public void ShowPage(TagContent bodyContent);
        public void ShowNewTemplate();
        public void ShowLoadTemplate();
        public void ShowFailure(string message);
        bool PickAFile(out string? path);
        void SwitchToModule(IScreenModule nextModule);
    }
}