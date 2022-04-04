using System.Numerics;
using Bot.Utilities.Processed.FieldInfo;
using Bot.Utilities.Processed.BallPrediction;
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
        public float throttle;
        private int closestBoostPad = 0;
        public float timeInState;
        public float startTime;


        public override bool IsViable(Bot agent, Packet packet, FieldInfo fieldInfo, BallPrediction prediction)
        {
            return packet.Players[agent.Index].Boost < 10;
        }

        public override Controller? GetOutput(Bot agent, Packet packet, FieldInfo fieldInfo, BallPrediction prediction)
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

            // If the AI is close to the boost lower throttle to make tighter turns else full throttle
            if (Vector3.Distance(carLocation, boostPadLocation) < 600)
            {
                throttle = 0.2f;
            }
            else
            {
                throttle = 1;
            }

            // If the AI has reached the boostpad then trigger state end condition.
            if (Vector3.Distance(carLocation, boostPadLocation) <= 30)
            {
                hasPickedUp = true;
            }

            if (startTime == default)
            {
                startTime = packet.GameInfo.SecondsElapsed;
            }

            timeInState = packet.GameInfo.SecondsElapsed - startTime;

            int enemyTeam;
            if (agent.Index == 0)
                enemyTeam = 1;
            else
                enemyTeam = 0;


            // If the AI has picked up a boost exit state
            if (hasPickedUp)
                return null;
            // If Take shot becomes viable exit
            else if (Vector3.Distance(packet.Players[agent.Index].Physics.Location, fieldInfo.Goals[enemyTeam].Location) < 5000)
                return null;
            // If save net becomes viable exit
            else if (Vector3.Distance(packet.Ball.Physics.Location, fieldInfo.Goals[agent.Team].Location) < 4000)
                return null;
            // If more than 5 seconds have passed since started state then exit state
            else if (timeInState >= 5)
                return null;
            // else follow controls to get to nearest boost
            else
                return new Controller { Throttle = throttle, Steer = steer };
        }
    }
}