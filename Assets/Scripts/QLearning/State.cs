using System.Collections.Generic;

namespace QLearning
{
    public class State
    {
        
        public int EnemyProximity { get; set; }   
        public int AmmoStatus { get; set; }     
        public int HealthStatus { get; set; }  
        
        // is it safe to shoot, considering the near zombies
        public int ZombieDensity { get; set; }
        
        //public int TimeSinceEncounter { get; set; }
        
        
        //State is based on below variables
        public State(int enemyDist, int ammo, int health, int density)
        {
            EnemyProximity = enemyDist;
            AmmoStatus = ammo;
            HealthStatus = health;
            ZombieDensity = density;
        }
        

        public string StateString()
        {
            return $"{EnemyProximity},{AmmoStatus},{HealthStatus},{ZombieDensity}";
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
                        for (var density = 0; density < 3; density++)
                        {
                            states.Add(new State(distance, ammo, health, density));
                        }
                        
                    }
                    
                }
            }
            
            return states;
        }
        
    }
}