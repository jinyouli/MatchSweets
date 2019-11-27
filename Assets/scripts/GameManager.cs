using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public class GameManager : MonoBehaviour
{
	public GameObject gridPrefab;
	public int xColumn;
	public int yRow;
	public float fillTime;

	public enum SweetType
	{
		EMPTY,
	    NORMAL,
		BARRIER,
		ROW_CLEAR,
		COLUMN_CLEAR,
		RAIBOWCANDY,
		COUNT
	}

	public Dictionary<SweetType,GameObject> sweetPrefabDict;

	[System.Serializable]
	public struct SweetPrefab
	{
		public SweetType type;
		public GameObject prefab;
	}

	public SweetPrefab[] sweetPrefabs;

	private GameSweet[,] sweets;
	private GameSweet pressedSweet;
	private GameSweet enteredSweet;

	private static GameManager _instance;
	public static GameManager Instance
	{
		get{
			return _instance;
		}
		set{
			_instance = value;
		}
	}


	public void Awake(){
		_instance = this;
	}


    // Start is called before the first frame update
    void Start()
    {
		sweetPrefabDict = new Dictionary<SweetType,GameObject>();
		for(int i=0; i < sweetPrefabs.Length; i++){
			if(!sweetPrefabDict.ContainsKey(sweetPrefabs[i].type)){
				sweetPrefabDict.Add(sweetPrefabs[i].type,sweetPrefabs[i].prefab);
			}
		}
		

        for(int x=0; x < xColumn; x++){
			for(int y=0; y < yRow; y++){
				GameObject chocolate = Instantiate(gridPrefab,CorrectPositionv2(x+0.5f,y+0.5f),Quaternion.identity);
				chocolate.transform.SetParent(transform);
			}
		}

		sweets = new GameSweet[xColumn, yRow];

		for(int x=0; x < xColumn; x++){
			for(int y=0; y < yRow; y++){
                CreateNewSweet(x,y,SweetType.EMPTY);
			}
		}

		StartCoroutine(AllFill());

		Destroy(sweets[4,4].gameObject);
        CreateNewSweet(4,4,SweetType.BARRIER);
    }


    // Update is called once per frame
    void Update()
    {
        
    }

	public Vector3 CorrectPosition(int x,int y){
		return new Vector3(transform.position.x - xColumn/2f + x , transform.position.y + yRow / 2f - y);
	}

	public Vector3 CorrectPositionv2(float x,float y){
		return new Vector3(transform.position.x - xColumn/2f + x , transform.position.y + yRow / 2f - y);
	}


    public GameSweet CreateNewSweet(int x,int y, SweetType type) {
        GameObject newSweet = Instantiate(sweetPrefabDict[type], CorrectPosition(x,y), Quaternion.identity);
        newSweet.transform.SetParent(transform);
        sweets[x, y] = newSweet.GetComponent<GameSweet>();
        sweets[x, y].Init(x, y, this, type);

        return sweets[x, y];
    }

    public IEnumerator AllFill() {

        bool needRefill = true;

        while (needRefill) {
            yield return new WaitForSeconds(fillTime);

            while (Fill())
            {
                yield return new WaitForSeconds(fillTime);
            }
            needRefill = ClearAllMatchedSweet();
        }
    }

    public bool Fill() {
        bool filledNotFinished = false;

        for (int y=yRow-2; y>=0; y--) {
            for (int x=0; x<xColumn; x++){
                GameSweet sweet = sweets[x,y];
                if (sweet.CanMove()) {
                    GameSweet sweetBelow = sweets[x,y+1];

                    if (sweetBelow.Type == SweetType.EMPTY) {
						Destroy(sweetBelow.gameObject);
                        sweet.MovedComponent.Move(x,y+1,fillTime);
                        sweets[x, y + 1] = sweet;
                        CreateNewSweet(x,y,SweetType.EMPTY);
                        filledNotFinished = true;
                    }
					else
					{
						for(int down=-1; down<=1;down ++){
							if(down!=0){
								int downX = x+down;
								if(downX >= 0 && downX < xColumn){
									GameSweet downSweet = sweets[downX, y+1];
									if(downSweet.Type == SweetType.EMPTY){
										bool canfill = true;
										for(int aboveY = y; aboveY >= 0; aboveY--){
											GameSweet sweetAbove = sweets[downX,aboveY];
											if(sweetAbove.CanMove()){
												break;
											}
											else if(!sweetAbove.CanMove() && sweetAbove.Type != SweetType.EMPTY){
												canfill = false;
												break;
											}
										}
										if(!canfill){
											Destroy(downSweet.gameObject);
											sweet.MovedComponent.Move(downX, y+1, fillTime);
											sweets[downX, y+1] = sweet;
											CreateNewSweet(x,y,SweetType.EMPTY);
											filledNotFinished = true;
											break;
										}
									}
								}
							}
						}
					}
                }
            }
        }

        for (int x=0; x<xColumn; x++) {
            GameSweet sweet = sweets[x,0];
            if (sweet.Type == SweetType.EMPTY) {
                GameObject newSweet = Instantiate(sweetPrefabDict[SweetType.NORMAL], CorrectPosition(x,-1),Quaternion.identity);
                newSweet.transform.parent = transform;
                int ranValue = UnityEngine.Random.Range(0, 6);

                sweets[x, 0] = newSweet.GetComponent<GameSweet>();
                sweets[x, 0].Init(x,-1,this,SweetType.NORMAL);
                sweets[x, 0].MovedComponent.Move(x,0,fillTime);
                sweets[x, 0].ColoredComponent.SetColor((ColorSweet.ColorType)ranValue);
                filledNotFinished = true;
            }
        }
        return filledNotFinished;
    }

    private bool isFriend(GameSweet sweet1, GameSweet sweet2){
		return (sweet1.X == sweet2.X && Mathf.Abs(sweet1.Y - sweet2.Y) == 1) || (sweet1.Y == sweet2.Y && Mathf.Abs(sweet1.X - sweet2.X) == 1);
	}

	private void ExchangeSweets(GameSweet sweet1, GameSweet sweet2){

        if (sweet1.CanMove() && sweet2.CanMove()){
			sweets[sweet1.X,sweet1.Y] = sweet2;
			sweets[sweet2.X,sweet2.Y] = sweet1;

            if (MatchSweets(sweet1,sweet2.X,sweet2.Y) != null || MatchSweets(sweet2,sweet1.X,sweet1.Y) != null){
				int tempX = sweet1.X;
				int tempY = sweet1.Y;

				sweet1.MovedComponent.Move(sweet2.X,sweet2.Y,fillTime);
				sweet2.MovedComponent.Move(tempX,tempY,fillTime);
				ClearAllMatchedSweet();
                StartCoroutine(AllFill());
            }
			else{
				sweets[sweet1.X,sweet1.Y] = sweet1;
				sweets[sweet2.X,sweet2.Y] = sweet2;
            }
		}
	}

	public void PressSweet(GameSweet sweet){
		pressedSweet = sweet;
	}
	
	public void EnterSweet(GameSweet sweet){
		enteredSweet = sweet;
	}

	public void ReleaseSweet(){
	
		if(isFriend(pressedSweet,enteredSweet)){
			ExchangeSweets(pressedSweet,enteredSweet);
		}
	}

	public List<GameSweet> MatchSweets(GameSweet sweet,int newX,int newY)
    {
        if (sweet.CanColor())
        {
            ColorSweet.ColorType color = sweet.ColoredComponent.Color;
            List<GameSweet> matchRowSweets = new List<GameSweet>();
            List<GameSweet> matchLineSweets = new List<GameSweet>();
            List<GameSweet> finishedMatchingSweets = new List<GameSweet>();

            //行匹配
            matchRowSweets.Add(sweet);

            //i=0代表往左，i=1代表往右
            for (int i = 0; i <=1; i++)
            {
                for (int xDistance = 1; xDistance < xColumn; xDistance++)
                {
                    int x;
                    if (i==0)
                    {
                        x = newX - xDistance;
                    }
                    else
                    {
                        x = newX + xDistance;
                    }
                    if (x<0||x>=xColumn)
                    {
                        break;
                    }

                    if (sweets[x,newY].CanColor()&&sweets[x,newY].ColoredComponent.Color==color)
                    {
                        matchRowSweets.Add(sweets[x, newY]);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (matchRowSweets.Count>=3)
            {
                for (int i = 0; i < matchRowSweets.Count; i++)
                {
                    finishedMatchingSweets.Add(matchRowSweets[i]);
                }
            }

            //L T型匹配
            //检查一下当前行遍历列表中的元素数量是否大于3
            if (matchRowSweets.Count>=3)
            {
                for (int i = 0; i < matchRowSweets.Count; i++)
                {
                    //行匹配列表中满足匹配条件的每个元素上下依次进行列遍历
                    // 0代表上方 1代表下方
                    for (int j = 0; j <=1; j++)
                    {
                        for (int yDistance = 1; yDistance < yRow; yDistance++)
                        {
                            int y;
                            if (j==0)
                            {
                                y = newY - yDistance;
                            }
                            else
                            {
                                y = newY + yDistance;
                            }
                            if (y<0||y>=yRow)
                            {
                                break;
                            }

                            if (sweets[matchRowSweets[i].X,y].CanColor()&&sweets[matchRowSweets[i].X,y].ColoredComponent.Color==color)
                            {
                                matchLineSweets.Add(sweets[matchRowSweets[i].X, y]);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    if (matchLineSweets.Count<2)
                    {
                        matchLineSweets.Clear();
                    }
                    else
                    {
                        for (int j = 0; j < matchLineSweets.Count; j++)
                        {
                            finishedMatchingSweets.Add(matchLineSweets[j]);
                        }
                        break;
                    }
                }
            }

            if (finishedMatchingSweets.Count>=3)
            {
                return finishedMatchingSweets;
            }

            matchRowSweets.Clear();
            matchLineSweets.Clear();

            matchLineSweets.Add(sweet);

            //列匹配

            //i=0代表往左，i=1代表往右
            for (int i = 0; i <= 1; i++)
            {
                for (int yDistance = 1; yDistance < yRow; yDistance++)
                {
                    int y;
                    if (i == 0)
                    {
                        y = newY - yDistance;
                    }
                    else
                    {
                        y = newY + yDistance;
                    }
                    if (y < 0 || y >= yRow)
                    {
                        break;
                    }
                    
                    if (sweets[newX, y].CanColor() && sweets[newX, y].ColoredComponent.Color == color)
                    {
                        matchLineSweets.Add(sweets[newX, y]);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (matchLineSweets.Count >= 3)
            {
                for (int i = 0; i < matchLineSweets.Count; i++)
                {
                    finishedMatchingSweets.Add(matchLineSweets[i]);
                }
            }

            //L T型匹配
            //检查一下当前行遍历列表中的元素数量是否大于3
            if (matchLineSweets.Count >= 3)
            {
                for (int i = 0; i < matchLineSweets.Count; i++)
                {
                    //行匹配列表中满足匹配条件的每个元素上下依次进行列遍历
                    // 0代表上方 1代表下方
                    for (int j = 0; j <= 1; j++)
                    {
                        for (int xDistance= 1; xDistance < xColumn; xDistance++)
                        {
                            int x;
                            if (j == 0)
                            {
                                x = newY - xDistance;
                            }
                            else
                            {
                                x = newY + xDistance;
                            }
                            if (x < 0 || x >= xColumn)
                            {
                                break;
                            }

                            if (sweets[x, matchLineSweets[i].Y].CanColor() && sweets[x, matchLineSweets[i].Y].ColoredComponent.Color == color)
                            {
                                matchRowSweets.Add(sweets[x, matchLineSweets[i].Y]);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    if (matchRowSweets.Count < 2)
                    {
                        matchRowSweets.Clear();
                    }
                    else
                    {
                        for (int j = 0; j < matchRowSweets.Count; j++)
                        {
                            finishedMatchingSweets.Add(matchRowSweets[j]);
                        }
                        break;
                    }
                }
            }

            if (finishedMatchingSweets.Count >= 3)
            {
                return finishedMatchingSweets;
            }
        }

        return null;
    }

	public bool ClearSweet(int x,int y){
        if (sweets[x,y].CanClear() && !sweets[x,y].ClearedComponent.IsClearing){
            sweets[x,y].ClearedComponent.Clear();
			CreateNewSweet(x,y,SweetType.EMPTY);
			return true;
		}

		return false;
	}

	private bool ClearAllMatchedSweet(){
		bool needRefill = false;
/*
        if (sweets[x, y].CanClear())
        {
            List<GameSweet> matchList = MatchSweets(sweets[x, y], x, y);
            if (matchList != null)
            {
                for (int i = 0; i < matchList.Count; i++)
                {
                    if (ClearSweet(matchList[i].X, matchList[i].Y))
                    {
                        needRefill = true;
                    }
                }
            }
        }
*/
        print("test123");
        for (int y=0;y<yRow;y++){
			for(int x=0;x<xColumn;x++){
				if(sweets[x,y].CanClear()){
					List<GameSweet> matchList = MatchSweets(sweets[x,y],x,y);

                    if (matchList!=null){
                        for (int i=0;i<matchList.Count;i++){
                            if (ClearSweet(matchList[i].X,matchList[i].Y)){
                                needRefill = true;
							}
						}
					}
				}
			}
		}

		return needRefill;
	}
}
