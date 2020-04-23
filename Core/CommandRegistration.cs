namespace Sacristan.Ahhnold.Core
{
    public class CommandRegistration
    {
        public delegate void CommandHandler(string[] args);

        public string command { get; private set; }
        public CommandHandler handler { get; private set; }
        public string help { get; private set; }

        public CommandRegistration(string command, CommandHandler handler, string help)
        {
            this.command = command;
            this.handler = handler;
            this.help = help;
        }
    }
}