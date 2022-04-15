using System.Numerics;
using Bot.Utilities.Processed.FieldInfo;
using Bot.Utilities.Processed.BallPrediction;
using Bot.Utilities.Processed.Packet;
using RLBotDotNet;

namespace Bot
{
    // State to chase after ball
    public class ChaseBall : BaseState
    {
        // Variables
        public float throttle;
        public float steer;
        public float pitch;
        public float yaw;
        public float roll;
        public bool jump;
        public bool boost;
        public bool handbrake;

        // Just hollow since state can only be called when no other state are viable.
        public override bool IsViable(Bot agent, Packet packet, FieldInfo fieldInfo, BallPrediction prediction)
        {
            return false;
        }

        public override Controller? GetOutput(Bot agent, Packet packet, FieldInfo fieldInfo, BallPrediction prediction)
        {
            // Output code here

            // Turn car towards ball
            Vector3 carLocation = packet.Players[agent.Index].Physics.Location;
            Vector3 ballLocation = packet.Ball.Physics.Location;
            Orientation carRotation = packet.Players[agent.Index].Physics.Rotation;

            Vector3 ballRelative = Orientation.RelativeLocation(carLocation, ballLocation, carRotation);


            // If ball is to the right steer left
            if (ballRelative.Y > 0)
            {
                steer = 1;
            }
            // If ball is to the left steer right
            else if (ballRelative.Y < 0)
            {
                steer = -1;
            }
            else if (ballRelative.Y == 0)
            {
                boost = true;
            }
            else boost = false;

            // States in order of importance
            BaseState[] states =
            {
                new KickOff(),
                new SaveNet(),
                new TakeShot(),
                new GetBoost(),
            };

            foreach (BaseState state in states)
                if (state.IsViable(agent, packet, fieldInfo, prediction))
                    return null;

            return new Controller {Throttle = 1 ,Steer = steer, Boost = boost };
        }
    }
}