using BasicImageFormFiller.Interfaces;
using Tag;

namespace BasicImageFormFiller.ModuleScreens
{
    public class WelcomeScreen : IScreenModule
    {
        public const string NewTemplate = "/template-new";
        public const string LoadTemplate = "/template-load";
        
        public void InterpretCommand(ITagModuleScreen moduleScreen, string command)
        {
            // TODO: new & open
            switch (command)
            {
                case NewTemplate:
                    moduleScreen.ShowNewTemplate();
                    break;
                case LoadTemplate:
                    moduleScreen.ShowLoadTemplate();
                    break;
            }
        }

        public TagContent StartScreen()
        {
            return T.g()[
                T.g("h1")["No document loaded"],
                T.g("p")["Use the menu to create a new document or load an existing one"],
                T.g("p")[
                    T.g("a", "href", NewTemplate)["Create new template"]
                ],
                T.g("p")[
                    T.g("a", "href", LoadTemplate)["Load existing template"]
                ]
            ];
        }

        public StateChangePermission StateChangeRequest() => StateChangePermission.Allowed;
        public void Activate() { }
    }
}