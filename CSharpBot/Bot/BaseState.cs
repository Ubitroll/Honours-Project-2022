using Bot.Utilities.Processed.FieldInfo;
using Bot.Utilities.Processed.Packet;
using RLBotDotNet;

namespace Bot
{
    //Base Class for states to inherit 
    public abstract class BaseState
    {
        // Determins if the state can run right now
        public abstract bool IsViable(Bot agent, Packet packet, FieldInfo fieldInfo);

        // Called each frame by StateHandler until returns null
        // Returns controller outputs tha the state should use and returns null when state finished
        public abstract Controller? GetOutput(Bot agent, Packet packet, FieldInfo fieldInfo);
    }
}