using System.Drawing;
using System.Windows.Media;
using Bot.Utilities.Processed.BallPrediction;
using Bot.Utilities.Processed.FieldInfo;
using Bot.Utilities.Processed.Packet;
using RLBotDotNet;

namespace Bot
{
    // We want to our bot to derive from Bot, and then implement its abstract methods.
    public class Bot : RLBotDotNet.Bot
    {
        private StateHandler stateHandler;
        public FieldInfo myCustomFieldInfo;
        public BallPrediction myCustomPrediction;

        public Bot(string name, int team, int index) : base(name, team, index)
        {

        }

        public override Controller GetOutput(rlbot.flat.GameTickPacket gameTickPacket)
        {
            myCustomFieldInfo = GetFieldInfo();
            myCustomPrediction = GetBallPrediction();
            stateHandler ??= new StateHandler(this, myCustomFieldInfo, myCustomPrediction); 

            Packet packet = new Packet(gameTickPacket);
            Controller output = stateHandler.GetOutput(packet);
            


            return output;
        }

        internal new FieldInfo GetFieldInfo() => new FieldInfo(base.GetFieldInfo());
        internal new BallPrediction GetBallPrediction() => new BallPrediction(base.GetBallPrediction());
    }
}