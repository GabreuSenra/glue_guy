using Sandbox;

public sealed class EndLine : Component
{   
    [Button ("Save value to Stats")]
    public void CheckLeaderboardResults()
    {
        Sandbox.Services.Stats.Increment( "MoveCount", LiderBoard.Instance.GetMoveCount());
        Sandbox.Services.Stats.Increment( "Time", LiderBoard.Instance.GetElapsedTime());
    }

    [Button ("Debug MoveCount Leaderboard")]
    public async void DebugMoveCountLeaderboardResults()
    {
        var board = Sandbox.Services.Leaderboards.GetFromStat( "gabreusenra.sticky_jumpers", "movecount" );

        board.SetAggregationMin(); // select the lowest value from each player
        //board.SetSortAscending(); // order by the lowest value first
        //board.FilterByMonth(); // only show results from this month
        //board.CenterOnMe(); // offset so I'm in the middle of the results

        board.MaxEntries = 5; 

        await board.Refresh();

        foreach ( var entry in board.Entries )
        {
            Log.Info( $"{entry.Rank} - {entry.DisplayName} - {entry.Value} [{entry.Timestamp}]" );
        }
    }

    [Button ("Debug Time Leaderboard")]
    public async void DebugTimeLeaderboardResults()
    {
        var board = Sandbox.Services.Leaderboards.GetFromStat( "gabreusenra.sticky_jumpers", "time" );

        board.SetAggregationMin(); // select the lowest value from each player
        //board.SetSortAscending(); // order by the lowest value first
        //board.FilterByMonth(); // only show results from this month
        //board.CenterOnMe(); // offset so I'm in the middle of the results

        board.MaxEntries = 5; 

        await board.Refresh();

        foreach ( var entry in board.Entries )
        {
            Log.Info( $"{entry.Rank} - {entry.DisplayName} - {entry.Value} [{entry.Timestamp}]" );
        }
    }
}