namespace QLearning
{
    public class State
    {
        //simplifying state space by making actions discrete
        public int EnemyDistance { get; set; }   // 0=Near, 1=Medium, 2=Far/None
        public int AmmoStatus { get; set; }      // 0=Empty, 1=Low(1-2), 2=HasAmmo(3+)
        public int HealthStatus { get; set; }    // 0=Critical, 1=Hurt, 2=Healthy
        
        public State() { }
        
        public State(int enemyDist, int ammo, int health)
        {
            EnemyDistance = enemyDist;
            AmmoStatus = ammo;
            HealthStatus = health;
        }
        
        
        // This string representation is used as the dictionary key in QValueStore
        public override string ToString()
        {
            return $"{EnemyDistance}_{AmmoStatus}_{HealthStatus}";
        }
        
        public override bool Equals(object obj)
        {
            if (obj is State other)
                return EnemyDistance == other.EnemyDistance &&
                       AmmoStatus == other.AmmoStatus &&
                       HealthStatus == other.HealthStatus;
            return false;
        }
        
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
        
        // Generate all possible states (3 x 3 x 3 = 27 total states)
        public static System.Collections.Generic.List<State> GetAllPossibleStates()
        {
            var states = new System.Collections.Generic.List<State>();
            for (int dist = 0; dist < 3; dist++)
            for (int ammo = 0; ammo < 3; ammo++)
            for (int health = 0; health < 3; health++)
                states.Add(new State(dist, ammo, health));
            return states;
        }
        
    }
}