using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour
 {
	/********\
	|settings|
	\********/
	public Tile startTile;
	public float movementSpeed;
	public string player1HorizontalAxis;
	public string player1VerticalAxis;
	public string player2HorizontalAxis;
	public string player2VerticalAxis;
	public string[] playerNames;
	public int flagScoreValue = 5;
	
	public AnimationCurve jumpCurve;
	
	/// <summary>
	/// How much time each player gets per turn.
	/// </summary>
	public float turnTime;
	
	/*other stuff*/	
	private const float NORTH = 0f;
	private const float EAST = 90f;
	private const float SOUTH = 180f;
	private const float WEST = 270f;
	
	private float _heading;
	private Tile _currentTile;
	private Tile _destination;
	private bool _initialised;
	
	private float   _currentMoveSpeed;
	private Vector3 _velocity;
	
	private bool _travelledThroughTeleporter;
	
	private Animator _playerAnimator;
	private Renderer _renderer;
	
	/// <summary>
	/// The _flags currently carried (not total or player score!)
	/// </summary>
	private int _flagsCarried = 0;
	
	private int[] _playerScores = {0,0};
	
	/// <summary>
	/// do not accept any movement change commands unless we're finished moving!
	/// </summary>
	private float _movementTimeRemaining;
	
	/// <summary>
	/// Time remaining before contorl switches to the other player;
	/// </summary>
	private float _playerTurnRemaining;
	
	public enum PLAYER_ID
	{
		PAINTER,
		COLLECTOR
	}
	
	private PLAYER_ID _currentPlayer;
	
	// Use this for initialization
	void Start ()
	{
		gameObject.transform.position = startTile.gameObject.transform.position;
		_currentPlayer = PLAYER_ID.COLLECTOR;

		_currentTile = startTile;
		_heading = NORTH;
		_movementTimeRemaining = 0;
		_initialised = false;
		_travelledThroughTeleporter = false;
		
		_playerAnimator = GetComponentInChildren<Animator>();
		_renderer = GetComponentInChildren<Renderer>();
	}
	
	// Update is called once per frame
	void Update ()
	{
		/*no guaruntees that Start will get called only after the tiles have been generated, so
		initialise here on the first frame*/
		if(!_initialised)
		{
			_initialised = true;
			_currentTile.OnTileEnter(_currentPlayer);
		}
		
		UpdateTurnSwitch();
		UpdateInput();	
		UpdateMovement();
	}
	
	void OnGUI()
	{
		//whose turn it is, time remaining:
		//TODO: make time remaining be formatted in minutes:seconds
		GUI.Box (new Rect (500, 200,100,50), playerNames[(int)_currentPlayer] + "\n" + _playerTurnRemaining);
		
		string scoreText = "";
		
		for(int i = 0; i < playerNames.Length; ++i)
		{
			scoreText += playerNames[i] + ": " + _playerScores[i] + "\n";
		}		
		
		//score:
		GUI.Box (new Rect (500, 400,100,50), scoreText);
	}

	/// <summary>
	/// Handle turn timer. Decrement time, switch controls if necessary.
	/// </summary>
	private void UpdateTurnSwitch()
	{
		_playerTurnRemaining -= Time.deltaTime;
		
		if(_playerTurnRemaining <= 0)
		{
		
			if(_currentPlayer == PLAYER_ID.PAINTER)
				_currentPlayer = PLAYER_ID.COLLECTOR;
			else
				_currentPlayer = PLAYER_ID.PAINTER;
			
			_playerTurnRemaining = turnTime;
			
			if(_movementTimeRemaining <= 0)
				_currentTile.OnTileEnter(_currentPlayer);
		}
	}
	
	private void UpdateInput()
	{
		//do not accept new movement command if we're currently in the process of moving!
		if(_movementTimeRemaining <= 0)
		{
			string horz = player1HorizontalAxis;
			string vert = player1VerticalAxis;
			
			if(_currentPlayer == PLAYER_ID.COLLECTOR)
			{
				horz = player2HorizontalAxis;
				vert = player2VerticalAxis;
			}
			
			//TODO: take heading into consideration here		
			if(Input.GetAxis (horz) < 0) //left
			{
				StartMovingTo(_currentTile.WestTile);		
			}
			else if(Input.GetAxis(horz) > 0) //right
			{
				StartMovingTo(_currentTile.EastTile);
			}
			else if(Input.GetAxis(vert) < 0) //down
			{
				StartMovingTo(_currentTile.SouthTile);
			}
			else if(Input.GetAxis(vert) > 0) //up
			{
				StartMovingTo(_currentTile.NorthTile);
			}
			
			//TODO: input for changing heading
		}
	}
	
	/// <summary>
	/// Abstract movement, in case we want to do fancy things with trajectory later.
	/// </summary>
	private void UpdateMovement()
	{
		if(_destination != null)
		{
			bool bJumping = _playerAnimator.GetBool("bJumping");
			
			Vector3 toDestination = (_destination.gameObject.transform.position - _currentTile.gameObject.transform.position);
			
			float speed = toDestination.magnitude / _currentMoveSpeed;
			
			gameObject.transform.position += toDestination.normalized * speed * Time.deltaTime;
			
			float yMod = Mathf.Abs (_destination.gameObject.transform.position.y - _currentTile.gameObject.transform.position.y);
			//float xMod = Mathf.Abs (_destination.gameObject.transform.position.x - _currentTile.gameObject.transform.position.x);
			float fEvalutateValue = 1 - (_movementTimeRemaining / _currentMoveSpeed);
			float fYOffset = jumpCurve.Evaluate(fEvalutateValue ) 
				* (yMod);
			gameObject.transform.position += new Vector3(0, fYOffset, 0);
			
			_movementTimeRemaining -= Time.deltaTime;
			//if(fYOffset > 0)
			//{
			//	Debug.Log (fEvalutateValue);
			//	Debug.Log (yMod);
			//	//Debug.Log (xMod);
			//	Debug.Log (fYOffset);
			//	
			//}
			if(_movementTimeRemaining < 0)
			{
				_movementTimeRemaining = 0;
				
				//cheating, just in case things don't line up:
				gameObject.transform.position = _destination.gameObject.transform.position;
				ArriveAtDestination();				
			}
			
		}
	}
	
	/// <summary>
	/// Begin movement to destination tile
	/// </summary>
	private void StartMovingTo(Tile destination)
	{
		if(destination != null)
		{
			//TODO: start playing movement anim
			
			_currentTile.OnTileExit(_currentPlayer);
			_destination = destination;
				
			Vector3 toDestination = (_destination.gameObject.transform.position - _currentTile.gameObject.transform.position);
			_currentMoveSpeed = movementSpeed * Mathf.Pow((toDestination.magnitude / 4), 0.5f);

			
			_movementTimeRemaining = _currentMoveSpeed;
			
			_playerAnimator.SetBool("bHaveDistination", true);
		} 
	}
		
	private void ArriveAtDestination()
	{
		
		_renderer.enabled = true;
		_playerAnimator.SetBool("bHaveDistination", false);
		_playerAnimator.SetBool("bJumping", false);
		
		HandleScoring(_destination);
		
		_currentTile = _destination;
		_destination = null;
		_currentTile.OnTileEnter(_currentPlayer); //if the current tile is a teleporter, this should start the TP anim
		
		//now cope with teleporters
		Tile specialDest = _currentTile.GetConnectedTeleporterTile();
		if(specialDest != null && _travelledThroughTeleporter == false)
		{
			//gameObject.transform.position = specialDest.gameObject.transform.position;
			///*NB: DO NOT treat this as arriving at a destination. otherwise you'll infinitely
			//teleport between the two, and overflow stack space.
			//
			//a jump pad CANNOT occupuy the same space as a teleporter (which you might want
			//with the idea of teleporting onto a jump pad), because then if you were to ordinarily
			//walk onto that jump pad/teleporter what would the correct action be? to jump or to
			//teleport?*/
			//
			//_currentTile = specialDest;
			//_currentTile.OnTileSpecialEnter(_currentPlayer);
			//HandleScoring(_currentTile);
			StartMovingTo(specialDest);
			_renderer.enabled = false;
			_travelledThroughTeleporter = true;
		}
		else
		{
			
			_travelledThroughTeleporter = false;
			
			
			specialDest = _currentTile.GetConnectedJumperTile();
			
			if(specialDest != null)
			{
				StartMovingTo(specialDest);
				_playerAnimator.SetBool("bJumping", true);
			}
		}
		
		
	}
	
	/// <summary>
	/// Paint the tile/pick up flags/score flags etc.
	/// </summary>
	private void HandleScoring(Tile scoreThisTile)
	{
		if(_currentPlayer == PLAYER_ID.PAINTER)
		{
			if(scoreThisTile.PaintTile())
			{
				++_playerScores[(int)_currentPlayer];
			}
		}
		else
		{
			_flagsCarried += scoreThisTile.CollectTileFlags();
			
			if(scoreThisTile.FlagGoalIsHere)
			{
				_playerScores[(int)_currentPlayer] += _flagsCarried * flagScoreValue;
				_flagsCarried = 0;
			}
		}
	}
		
}
