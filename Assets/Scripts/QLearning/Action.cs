namespace QLearning
{
    public class Action
    {
        
        //Set up actions for what the agent can do
        public enum ActionType
        {
            MoveToEnemy,      // 0
            FleeFromEnemy,    // 1
            MoveToPickup,     // 2
            Shoot             // 3
        }
        
        public ActionType Type{ get; set; }
        
        public Action() { }
        
        public Action(ActionType type)
        {
            Type = type;
        }
        
        
        public override string ToString()
        {
            return Type.ToString();
        }
        
        public override bool Equals(object obj)
        {
            if (obj is Action other)
                return Type == other.Type;
            return false;
        }
        
        public override int GetHashCode()
        {
            return Type.GetHashCode();
        }
        
        
    }
}