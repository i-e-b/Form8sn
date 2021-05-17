using Tag;

namespace BasicImageFormFiller
{
    internal interface IScreenModule
    {
        void InterpretCommand(ITagModuleScreen moduleScreen, string command);
        TagContent StartScreen();
    }
}