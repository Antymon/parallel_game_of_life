public interface ICellState
{
    void SetActive();
}

public interface ICellCoordinates
{

}

public interface IBoard<StateType, CoordinatesType> 
    where StateType : ICellState 
    where CoordinatesType : ICellCoordinates
{
    StateType GetState(CoordinatesType coordinates);
    StateType[] GetNeighboursStates(CoordinatesType coordinates);
    void SetState(CoordinatesType coordinates, StateType state);
    void Copy(IBoard<StateType, CoordinatesType> source, CoordinatesType from, CoordinatesType to);
}


public interface IRule<StateType> where StateType : ICellState
{
    StateType Apply(StateType currentState, StateType[] neigboursStates);
}

public interface ICellularAutomaton<StateType, CoordinatesType> 
    where StateType : ICellState 
    where CoordinatesType : ICellCoordinates
{
    IBoard<StateType, CoordinatesType> Board { get; }
    IRule<StateType> Rule { get; }
    StateType NextState(CoordinatesType coordinates);
}

public class CellularAutomaton<StateType, CoordinatesType> : ICellularAutomaton<StateType, CoordinatesType> 
    where StateType : ICellState 
    where CoordinatesType : ICellCoordinates
{
    public CellularAutomaton(IBoard<StateType, CoordinatesType> board, IRule<StateType> rule)
    {
        Board = board;
        Rule = rule;
    }

    public IBoard<StateType, CoordinatesType> Board { get; private set; }
    public IRule<StateType> Rule { get; private set; }

    public StateType NextState(CoordinatesType coordinates)
    {
        var currentState = Board.GetState(coordinates);
        var neigbourStates = Board.GetNeighboursStates(coordinates);
        return Rule.Apply(currentState, neigbourStates);
    }
}