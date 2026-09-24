using System.Collections.Generic;

namespace QLearning
{
    public class State
    {
        
        public int EnemyProximity { get; set; }   
        public int AmmoStatus { get; set; }     
        public int HealthStatus { get; set; }  
        
        // 1 if the agent is close to a wall / NavMesh edge (cornered risk)
        public int NearWall { get; set; }
        
        public int PickupDist { get; set; }
        
        //public int TimeSinceEncounter { get; set; }
        
        
        //State is based on below variables
        public State(int enemyDist, int ammo, int health, int nearWall, int pickupDist)
        {
            EnemyProximity = enemyDist;
            AmmoStatus = ammo;
            HealthStatus = health;
            NearWall = nearWall;
            PickupDist = pickupDist;
        }
        

        public string StateString()
        {
            return $"{EnemyProximity},{AmmoStatus},{HealthStatus},{NearWall},{PickupDist}";
        }
        
        
        // Sets up all possible states
        public static List<State> GetAllPossibleStates()
        {
            var states = new List<State>();
            
            for (var distance = 0; distance < 3; distance++)
            {
                for (var ammo = 0; ammo < 3; ammo++)
                {
                    for (int health = 0; health < 3; health++)
                    {
                        for (var nearWall = 0; nearWall < 2; nearWall++)
                        {
                            for (var pickupDist = 0; pickupDist < 3; pickupDist++)
                            {
                                states.Add(new State(distance, ammo, health, nearWall, pickupDist));
                            }
                           
                        }
                        
                    }
                    
                }
            }
            
            return states;
        }
        
    }
}