using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

namespace Agents
{
    public class MoveToGoalAgent : Agent
    {
        
        [SerializeField] private Vector3 startPosition;
        
        [SerializeField] private Transform goalTransform;
        
        [SerializeField] private float moveSpeed = 1f;

        //Called as soon as episode begins
        //allowing us to reset everything
        public override void OnEpisodeBegin()
        {
            transform.position = startPosition;
        }
        

        //collect observations from environment, data AI agent needs
        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(transform.position);
            sensor.AddObservation(goalTransform.position);
            
        }
        
        
        //contains actions as floats or ints
        public override void OnActionReceived(ActionBuffers actions)
        {
            //defining movement 
            //AI only feeds numbers, then we adjust
            float moveX = actions.ContinuousActions[0];
            float moveZ = actions.ContinuousActions[1];
            
            transform.position += new Vector3(moveX,0,moveZ) * Time.deltaTime * moveSpeed;
        }

        
        //Testing, and allows us to modify the actions
        //check behavior type
        public override void Heuristic(in ActionBuffers actionsOut)
        {
            ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
            
            //Configure to use new input system
            continuousActions[0] = Input.GetAxis("Horizontal");
            continuousActions[1] = Input.GetAxis("Vertical");
            
        }


        //On trigger enter provides us with our reward
        private void OnTriggerEnter(Collider other)
        {

            if (other.gameObject.CompareTag("Wall"))
            {
                SetReward(-1.0f);
                EndEpisode();
            }

            if (other.gameObject.CompareTag("GoalObject"))
            {
                SetReward(1.0f);
                EndEpisode();
            }
            
        
        }
        
    
    }
    
    
    
  
}
