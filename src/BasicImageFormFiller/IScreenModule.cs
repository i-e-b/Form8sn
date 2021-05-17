using Tag;

namespace BasicImageFormFiller
{
    public interface IScreenModule
    {
        void InterpretCommand(ITagModuleScreen moduleScreen, string command);
        TagContent StartScreen();
        StateChangePermission StateChangeRequest();
    }

    public enum StateChangePermission
    {
        NotAllowed, Allowed
    }
}