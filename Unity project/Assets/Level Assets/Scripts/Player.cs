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
	public string player1RotateAxis;
	public string player2RotateAxis;
	public string[] playerNames;
	
	public GUIStyle turnTimerStyle;
		
	public GameObject prefabCandleGoalParticle;
	public GameObject prefabCandlePickupParticle;
	
	/// <summary>
	/// How many frames rotating the player model should take up.
	/// </summary>
	public int framesPerRotate = 5;
	
	private int _rotationFramesRemaining = 0;
	
	public AnimationCurve jumpCurve;
	
	/// <summary>
	/// How much time each player gets per turn.
	/// </summary>
	public float turnTime;
	
	/// <summary>
	/// Used to determine input directions. This is what direction the camera is facing,
	/// or currently rotating to face.
	/// </summary>
	private int _cameraTargetHeading;
	
	/// <summary>
	/// Used to determine what direction to face the player model ONLY. Camera heading
	/// handled seperately.
	/// </summary>
	private int _playerTargetHeading;
	
	/// <summary>
	/// How much to rotate the player model, in degrees, each frame (if currently rotating).
	/// </summary>
	private float _rotateModelThisAmount;
	
	private Tile _currentTile;
	private Tile _destination;
	private bool _initialised;
	
	private float _currentMoveSpeed;
	private Vector3 _velocity;
	
	private bool _travelledThroughTeleporter;
	
	private Animator _playerAnimator;
	private Renderer _renderer;
	
	private Camera _camera;
	private Quaternion _cameraTargetRotation;
	
	/// <summary>
	/// child
	/// </summary>	
	private Transform _cameraPivot;
	
	/// <summary>
	/// child
	/// </summary>
	private Transform _playerModel;	
	
	private AudioSource _playerPainterLoop;
	private AudioSource _playerCounterLoop;
	private AudioSource _playerChangeSound;
	private AudioSource _playerPutDownCandle;
	
	private bool _have5Candle;		
	private bool _have10Candle;		
	private bool _have15Candle;		
	private bool _have25Candle;
		
	public Texture FullCandleSlot5;
	public Texture EmptyCandleSlot5;
	public Texture FullCandleSlot10;
	public Texture EmptyCandleSlot10;
	public Texture FullCandleSlot15;
	public Texture EmptyCandleSlot15;
	public Texture FullCandleSlot25;
	public Texture EmptyCandleSlot25;
	
	/// <summary>
	/// Stops multiple inputs for rotate.
	/// </summary>	
	private bool _rotatedThisFrame;
		
	private bool _teleporting;
	
	public GameObject CandleBackpackPrefab;
	private GameObject _candleBackpack;
	private GameObject _candleHolster;
		
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
		_teleporting = false;
		
		gameObject.transform.position = startTile.gameObject.transform.position;
		_currentPlayer = PLAYER_ID.COLLECTOR;

		_currentTile = startTile;
		_cameraTargetHeading = 0;
		_playerTargetHeading = _cameraTargetHeading;
		_movementTimeRemaining = 0;
		_initialised = false;
		_travelledThroughTeleporter = false;
		
		_cameraTargetHeading = 0;
		
		_playerAnimator = GetComponentInChildren<Animator>();
		_camera = GetComponentInChildren<Camera>();
		_renderer = GetComponentInChildren<Renderer>();
		
		_cameraPivot = gameObject.transform.FindChild("CameraPivot");
		
		_playerModel = gameObject.transform.FindChild("Player-RIG");
		
		_playerPainterLoop = transform.FindChild ("Sounds").transform.FindChild("PlayerOneLoop").GetComponent<AudioSource>();
		_playerCounterLoop = transform.FindChild ("Sounds").transform.FindChild("PlayerTwoLoop").GetComponent<AudioSource>();
		_playerChangeSound = transform.FindChild ("Sounds").transform.FindChild("PlayerChange").GetComponent<AudioSource>();
		_playerPutDownCandle = transform.FindChild ("Sounds").transform.FindChild("PlayerPutDownCandle").GetComponent<AudioSource>();
		
		_have5Candle = false;
		_have10Candle = false;
		_have15Candle = false;
		_have25Candle = false;	
		
		_candleHolster = _playerModel.FindChild("CandleHolster").gameObject;
		_candleBackpack = null;
	}
	
	// Update is called once per frame
	void Update ()
	{
		/*no guarantees that Start will get called only after the tiles have been generated, so
		initialise here on the first frame*/
		if(!_initialised)
		{
			_initialised = true;
			_currentTile.OnTileEnter(_currentPlayer);
		}
		
		UpdateTurnSwitch();
		
		_cameraTargetRotation = Quaternion.AngleAxis(_cameraTargetHeading, Vector3.up);
		_cameraPivot.rotation = Quaternion.Slerp(_cameraPivot.rotation, _cameraTargetRotation, Time.deltaTime * 2.5f);

			
		if(!_teleporting)
		{
			UpdateInput();
			UpdateMovement();
		}
	}
	
	void OnGUI()
	{		
		//candles carried:
		float textureWidth = 32;
		float margin = 5;
		
		//5 candle
		GUI.Label(new Rect(Screen.width - (textureWidth + margin),
			Screen.height - 4*(textureWidth + margin),
			textureWidth,
			textureWidth),
			_have5Candle?FullCandleSlot5:EmptyCandleSlot5);
		
		//10 candle
		GUI.Label(new Rect(Screen.width - (textureWidth + margin),
			Screen.height - 3*(textureWidth + margin),
			textureWidth,
			textureWidth),
			_have10Candle?FullCandleSlot10:EmptyCandleSlot10);
		
		//15 candle
		GUI.Label(new Rect(Screen.width - (textureWidth + margin),
			Screen.height - 2*(textureWidth + margin),
			textureWidth,
			textureWidth),
			_have15Candle?FullCandleSlot15:EmptyCandleSlot15);
		
		//25 candle
		GUI.Label(new Rect(Screen.width - (textureWidth + margin),
			Screen.height - (textureWidth + margin),
			textureWidth,
			textureWidth),
			_have25Candle?FullCandleSlot25:EmptyCandleSlot25);
		
		//whose turn it is, time remaining before player switch:
		const float turnTimerWidth = 200;
		
		GUI.Label (new Rect (Screen.width/2 - turnTimerWidth/2, LevelController.hudMargin*2 + LevelController.timerBoxHeight, turnTimerWidth,50),
			playerNames[(int)_currentPlayer] + "\n" +_playerTurnRemaining.ToString ("F2"), turnTimerStyle); //add player style.
	}

	/// <summary>
	/// Handle turn timer. Decrement time, switch controls if necessary.
	/// </summary>
	private void UpdateTurnSwitch()
	{
		_playerTurnRemaining -= Time.deltaTime;
		
		if(_playerTurnRemaining <= 0)
		{
			if(_teleporting || _destination != null)
			{
				_playerTurnRemaining = 0.0f;
				return; //skip the rest of the code so that we don't transition to another player until we arrive
			}
			
			_playerChangeSound.Play();
		
			if(_currentPlayer == PLAYER_ID.PAINTER)
			{
				_currentPlayer = PLAYER_ID.COLLECTOR;
				
				float fMusicTime = _playerPainterLoop.time;
				_playerPainterLoop.Stop();
				_playerCounterLoop.time = fMusicTime;
				_playerCounterLoop.Play();
			}
			else
			{
				_currentPlayer = PLAYER_ID.PAINTER;
				
				float fMusicTime = _playerCounterLoop.time;
				_playerCounterLoop.Stop();
				_playerPainterLoop.time = fMusicTime;
				_playerPainterLoop.Play();
			}
			
			_playerTurnRemaining = turnTime;
			
			if(_movementTimeRemaining <= 0)
			{
				_destination = _currentTile;
				ArriveAtDestination();
			}
		}
	}
	
	private void UpdateInput()
	{
		string horz = player1HorizontalAxis;
		string vert = player1VerticalAxis;
		string rotate = player1RotateAxis;
		
		if(_currentPlayer == PLAYER_ID.COLLECTOR)
		{
			horz = player2HorizontalAxis;
			vert = player2VerticalAxis;
			rotate = player2RotateAxis;
		}
		
		//do not accept new movement command if we're currently in the process of moving!
		if(_destination == null)
		{

			Tile[] exits = {_currentTile.NorthTile,
							_currentTile.EastTile,
							_currentTile.SouthTile,
							_currentTile.WestTile};
			
			int headingOffset = _cameraTargetHeading / 90;
			
			if(Input.GetAxis(vert) > 0) //up
			{
				StartMovingTo(exits[headingOffset], (_cameraTargetHeading + 0) % 360);
			}
			else if(Input.GetAxis(horz) > 0) //right
			{
				StartMovingTo(exits[(headingOffset + 1) % 4], (_cameraTargetHeading + 90) % 360);
			}
			else if(Input.GetAxis(vert) < 0) //down
			{
				StartMovingTo(exits[(headingOffset + 2) % 4], (_cameraTargetHeading + 180) % 360);
			}
			else if(Input.GetAxis(horz) < 0) //left
			{
				StartMovingTo(exits[(headingOffset + 3) % 4],(_cameraTargetHeading + 270) % 360);
			}
			else
			{
				_playerAnimator.SetBool("bHaveDestination", false);
			}
			
		}
		
		_cameraTargetRotation = Quaternion.AngleAxis(_cameraTargetHeading, Vector3.up);
		_cameraPivot.rotation = Quaternion.Slerp(_cameraPivot.rotation, _cameraTargetRotation, Time.deltaTime * 2.5f);
		
		if(Input.GetAxis(rotate) < 0)
		{
			if(!_rotatedThisFrame)
			{
				_rotatedThisFrame = true;
				
				_cameraTargetHeading -= 90;
				
				if(_cameraTargetHeading < 0)
					_cameraTargetHeading = 270;
			}
		}
		else if(Input.GetAxis(rotate) > 0)
		{
			if(!_rotatedThisFrame)
			{
				_rotatedThisFrame = true;
				
				_cameraTargetHeading += 90;
				if(_cameraTargetHeading >= 360)
					_cameraTargetHeading = 0;
			}
		}
		else
		{
			_rotatedThisFrame = false;
		}
		
	}
	
	/// <summary>
	/// Handle movement, including rotating player model.
	/// </summary>
	private void UpdateMovement()
	{
		if(_destination != null)
		{
			
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

		if(_rotationFramesRemaining > 1)
		{
			_playerModel.Rotate(new Vector3(0, _rotateModelThisAmount, 0));
			
			_rotationFramesRemaining--;
		}
		else
		{
			_playerModel.rotation = Quaternion.AngleAxis(_playerTargetHeading, Vector3.up);
		}
	}
	
	/// <summary>
	/// Begin movement to destination tile. Turn if necessary.
	/// </summary>
	private void StartMovingTo(Tile destination, int newPlayerHeading)
	{
		if(destination != null)
		{
			_currentTile.OnTileExit(_currentPlayer);
			_destination = destination;
				
			Vector3 toDestination = (_destination.gameObject.transform.position - _currentTile.gameObject.transform.position);
			_currentMoveSpeed = movementSpeed * Mathf.Pow((toDestination.magnitude / 4), 0.5f);

			_movementTimeRemaining = _currentMoveSpeed;
			
			_playerAnimator.SetBool("bHaveDestination", true);
			
			if(_destination.transform.position.y < _currentTile.transform.position.y)
			{
				_playerAnimator.SetBool("bHaveDestination", false);
				_playerAnimator.SetBool("bFalling", true);
			}
			
			//if not heading the same way as last time, start to rotate the player model.
			if(_playerTargetHeading != newPlayerHeading)
			{
				_rotateModelThisAmount = newPlayerHeading - _playerTargetHeading;
				if(_rotateModelThisAmount > 180)
				{
					_rotateModelThisAmount *= -1; //turn left instead of right.
				}
				
				_rotateModelThisAmount /= framesPerRotate;
				
				_rotationFramesRemaining = framesPerRotate;					
			}
			_playerTargetHeading = newPlayerHeading;
		} 
	}
		
	private void ArriveAtDestination()
	{
		//_renderer.enabled = true;
		_playerAnimator.SetBool("bJumping", false);
		_playerAnimator.SetBool("bFalling", false);
		
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
			//a jump pad CANNOT occupy the same space as a teleporter (which you might want
			//with the idea of teleporting onto a jump pad), because then if you were to ordinarily
			//walk onto that jump pad/teleporter what would the correct action be? to jump or to
			//teleport?*/
			//
			//_currentTile = specialDest;
			//_currentTile.OnTileSpecialEnter(_currentPlayer);
			//HandleScoring(_currentTile);
			_playerAnimator.SetBool("bHaveDestination", false);
			StartCoroutine(DoTeleport(specialDest));
			_travelledThroughTeleporter = true;
		}
		else
		{
			
			_travelledThroughTeleporter = false;
			
			specialDest = _currentTile.GetConnectedJumperTile();
			
			if(specialDest != null)
			{
				StartMovingTo(specialDest, _playerTargetHeading);
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
				GameManager.GetInstance().Player1Score++; //TODO: enum vs hardcoded? terrible, clean up if time.
			}
		}
		else
		{
			int iSpawnerHere = scoreThisTile.IsSpawnerHere();
			
			if(_candleBackpack == null && iSpawnerHere != 0)
			{
				_candleBackpack = Instantiate(CandleBackpackPrefab) as GameObject;
				_candleBackpack.transform.parent = _candleHolster.transform;
				_candleBackpack.transform.localPosition = new Vector3(0, 0, 0);
				_candleBackpack.transform.localRotation = Quaternion.identity;
			}
			
			if(iSpawnerHere == 5 && !_have5Candle)
			{
				_have5Candle = true;
				scoreThisTile.CollectTileCandle();
				
			}
			if(iSpawnerHere == 10 && !_have10Candle)
			{
				_have10Candle = true;
				scoreThisTile.CollectTileCandle();
				
				GameObject obj = Instantiate(prefabCandlePickupParticle) as GameObject;
				obj.transform.parent = gameObject.transform;
				obj.transform.localPosition = new Vector3(0.0f, 0.0f, 5.0f);
			}
			if(iSpawnerHere == 15 && !_have15Candle)
			{
				_have15Candle = true;
				scoreThisTile.CollectTileCandle();
				
				GameObject obj = Instantiate(prefabCandlePickupParticle) as GameObject;
				obj.transform.parent = gameObject.transform;
				obj.transform.localPosition = new Vector3(0.0f, 0.0f, 5.0f);
			}
			if(iSpawnerHere == 25 && !_have25Candle)
			{
				_have25Candle = true;
				scoreThisTile.CollectTileCandle();
				
				GameObject obj = Instantiate(prefabCandlePickupParticle) as GameObject;
				obj.transform.parent = gameObject.transform;
				obj.transform.localPosition = new Vector3(0.0f, 0.0f, 5.0f);
			}
			
			if(scoreThisTile.FlagGoalIsHere)
			{
				if(_have5Candle || _have10Candle || _have15Candle || _have25Candle)
				{
					GameObject obj = Instantiate(prefabCandleGoalParticle) as GameObject;
					obj.transform.position = scoreThisTile.transform.position;
					
					if(_candleBackpack)
					{
						DestroyObject(_candleBackpack);
						_candleBackpack = null;
					}
					
					_playerPutDownCandle.Play();
				}
				if(_have5Candle)
				{
					GameManager.GetInstance().Player2Score += 5;
					_have5Candle = false;
				}
				
				if(_have10Candle)
				{
					GameManager.GetInstance().Player2Score += 10;
					_have10Candle = false;
				}
				
				if(_have15Candle)
				{
					GameManager.GetInstance().Player2Score += 15;
					_have15Candle = false;
				}
				
				if(_have25Candle)
				{
					GameManager.GetInstance().Player2Score += 25;
					_have25Candle = false;
				}
			}
		}
	}
	
	private IEnumerator DoTeleport(Tile destination)
	{
		
		_teleporting = true;
		
		_playerAnimator.SetBool("bTeleporting", true);
		yield return new WaitForSeconds(0.25f);
		
		Renderer[] renderers = GetComponentsInChildren<Renderer>() as Renderer[];
		for(int i = 0;i < renderers.Length;++i)
		{
			renderers[i].enabled = false;
		}
		
		yield return new WaitForSeconds(0.15f);
		
		float fDistFromTarget = (destination.transform.position - transform.position).magnitude;
		while(fDistFromTarget > 1)
		{
			
			gameObject.transform.position += (destination.transform.position - transform.position).normalized;
			fDistFromTarget = (destination.transform.position - transform.position).magnitude;
			yield return new WaitForSeconds(0.001f);
		}
		
		gameObject.transform.position = destination.transform.position;
		_currentTile = destination;
		_currentTile.OnTileSpecialEnter(_currentPlayer);
		yield return new WaitForSeconds(0.25f);
		
		for(int i = 0;i < renderers.Length;++i)
		{
			renderers[i].enabled = true;
		}
		
		_playerAnimator.SetBool("bTeleporting", false);
		
		_teleporting = false;
	}
		
}
