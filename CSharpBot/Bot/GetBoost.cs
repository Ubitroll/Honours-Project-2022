using System.Numerics;
using Bot.Utilities.Processed.FieldInfo;
using Bot.Utilities.Processed.Packet;
using RLBotDotNet;

namespace Bot
{
    // State to retreive Boost
    public class GetBoost : BaseState
    {
        // Variables        
        public bool hasPickedUp = false;
        public float steer;
        private int closestBoostPad = 0;


        public override bool IsViable(Bot agent, Packet packet, FieldInfo fieldInfo)
        {
            return packet.Players[agent.Index].Boost < 80;
        }

        public override Controller? GetOutput(Bot agent, Packet packet, FieldInfo fieldInfo)
        {
            // Output code here

            // Get datarequired to drive to boost pad.
            Vector3 carLocation = packet.Players[agent.Index].Physics.Location;
            Orientation carRotation = packet.Players[agent.Index].Physics.Rotation;

            // Get location of all boost pads
            for (int i = 0; i < fieldInfo.BoostPads.Length; i++)
            {
                // if the booost pad is active.
                if (packet.BoostPadStates[i].IsActive)
                {
                    // if current pad is closer than closest pad set to closest pad
                    if (Vector3.Distance(carLocation, fieldInfo.BoostPads[i].Location) < Vector3.Distance(carLocation, fieldInfo.BoostPads[closestBoostPad].Location))
                    {
                        closestBoostPad = i;
                    }
                }
            }

            Vector3 boostPadLocation = fieldInfo.BoostPads[closestBoostPad].Location;

            // Find where boost pad is relative to AI
            Vector3 ballRelativeLocation = Orientation.RelativeLocation(carLocation, boostPadLocation, carRotation);

            // If Boost pad is to our left we steer left otherwise steer right..
            if (ballRelativeLocation.Y > 0)
            {
                steer = 1;
            }
            else
            {
                steer = -1;
            }

            // If the AI has reached the boostpad then trigger state end condition.
            if (Vector3.Distance(carLocation, boostPadLocation) == 0)
            {
                hasPickedUp = true;
            }


            // If the AI has picked up a boost exit state
            if (hasPickedUp)
                return null;
            // else follow controls to get to nearest boost
            else
                return new Controller { Throttle = 1, Steer = steer };
        }
    }
}