namespace QLearning
{
    public class Action
    {
        
        public enum ActionType
        {
            HoldPosition,
            Flee,    
            MoveToPickup,   
            Shoot,
            Dash
        }
        
        public ActionType Type{ get; set; }
        
        public Action() { }
        
        public Action(ActionType type)
        {
            Type = type;
        }
        
        
    }
}