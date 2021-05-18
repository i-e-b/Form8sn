using Tag;

namespace BasicImageFormFiller.Interfaces
{
    public interface IScreenModule
    {
        /// <summary>
        /// User clicked on a link while this module was active.
        /// Take any appropriate action.
        /// </summary>
        void InterpretCommand(ITagModuleScreen moduleScreen, string command);
        
        /// <summary>
        /// Show the base screen for this module
        /// </summary>
        TagContent StartScreen();
        
        /// <summary>
        /// Allows the module to prevent accidental closure if something is in progress
        /// </summary>
        StateChangePermission StateChangeRequest();
        
        /// <summary>
        /// Notify this module that a sub-process has finished
        /// </summary>
        void Activate();
    }

    public enum StateChangePermission
    {
        NotAllowed, Allowed
    }
}