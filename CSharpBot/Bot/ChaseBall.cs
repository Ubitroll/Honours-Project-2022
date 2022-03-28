using System.Numerics;
using Bot.Utilities.Processed.FieldInfo;
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
        public override bool IsViable(Bot agent, Packet packet, FieldInfo fieldInfo)
        {
            return false;
        }

        public override Controller? GetOutput(Bot agent, Packet packet, FieldInfo fieldInfo)
        {
            // Output code here

            // Turn car towards ball
            Vector3 carLocation = packet.Players[agent.Index].Physics.Location;
            Vector3 ballLocation = packet.Ball.Physics.Location;
            Orientation carRotation = packet.Players[agent.Index].Physics.Rotation;

            Vector3 ballRelative = Orientation.RelativeLocation(carLocation, ballLocation, carRotation);

            if (ballRelative.Y > 0)
            {
                steer = 1;
            }
            else
            {
                steer = -1;
            }

            return new Controller {Throttle = 1 ,Steer = steer, Boost = true };
        }
    }
}