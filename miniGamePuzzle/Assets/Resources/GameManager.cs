using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public GameObject panelComplete;
    
    enum STATE { wait, idle, touch, move, calc, finish, cancel}
    STATE state = STATE.wait;

    int sliceCnt = 3;  // 퍼즐 분할 수 (? x ?)
    int imgNum = 0;

    float tileScale; // 기존 이미지에 대한 비율 조정을 위한 변수
    float tileSpan; // 타일 간격

    Transform origin;

    Text txtTime;
    Text txtMove;

    int moveCnt = 0;
    float startTime; 
    float timeSpan;  

    bool canUI = true;

    List<Sprite> sprites = new List<Sprite>(); // 자른 이미지를 저장할 List
    List<Transform> tiles = new List<Transform>(); // 화면에 배치할 Tile List

    List<int> orders = new List<int>();  // 각각의 타일 번호를 저장할 List
    List<int> moveTiles = new List<int>(); // 이동해야 할 타일 번호를 저장할 List 
 
    // 타일 정보에 대한 검색 및 추가에 배열 보다는 List를 사용

    int dir; // 타일 이동 방향 up, down, left, right
    int tileNum; // 클릭한 타일 번호
    bool canCalc = true; // 타일 이동 후 갱신을 할 필요가 있는가?

    void SetUI()
    {
        panelComplete.SetActive(false);

        txtTime = GameObject.Find("TxtTime").GetComponent<Text>();
        txtMove = GameObject.Find("TxtMove").GetComponent<Text>();
    }

    void SetTime()
    {
        timeSpan = Time.time - startTime;
        int h = Mathf.FloorToInt(timeSpan / 3600);
        int m = Mathf.FloorToInt(timeSpan / 60 % 60);
        float s = timeSpan % 60;

        txtTime.text = string.Format("Time : {0:0} : {1:0} : {2:0}", h, m, s);
        txtMove.text = moveCnt.ToString("Move : 0");
    }
    void SplitTexture() // 타일을 분할하기 위한 함수
    {
        Texture2D org = Resources.Load("Image_0", typeof(Texture2D)) as Texture2D; // 기준 이미지를 로드

        tileScale = (float)org.width / org.width; // 기준 이미지 비율을 조정하여 tilsScale에 저장

        // 자를 조각의 크기
        float w = org.width / sliceCnt; 
        float h = org.height / sliceCnt;

        sprites.Clear();
        for(int y = sliceCnt - 1; y >= 0; y--) 
        {
            for(int x = 0; x < sliceCnt; x++)
            {
                Rect rect = new Rect(x * w, y * h, w, h); // 분할 되는 이미지의 기준점과 크기
                Vector2 pivot = new Vector2(0, 1); // 잘라낸 이미지의 Pivot

                Sprite sprite = Sprite.Create(org, rect, pivot);
                sprites.Add(sprite);
            }
        }
    }

    void MakeSingleTile(int idx, Vector2 size)
    {
        GameObject tile = Instantiate(Resources.Load("Tile")) as GameObject;
        tile.transform.localScale = new Vector3(tileScale, tileScale, 1);

        SpriteRenderer render = tile.GetComponent<SpriteRenderer>();
        render.sprite = sprites[idx];

        render.material.SetInt("_count", sliceCnt);
        tile.name = "Tile" + idx;

        BoxCollider2D collider = tile.GetComponent<BoxCollider2D>();
        collider.size = size;
        collider.offset = new Vector2(size.x / 2, -size.y / 2);

        tiles.Add(tile.transform);
    }

    void InitGame()
    {
        SplitTexture();
        MakeTiles();
        SetUI();
    }

    void MakeTiles()
    { 
        tiles.Clear();
        orders.Clear();

        Vector2 size = sprites[0].bounds.size;
        int n = 0;

        for(int y = 0; y < sliceCnt; y++)
        {
            for(int x = 0; x < sliceCnt; x++)
            {
                MakeSingleTile(n, size);
                orders.Add(n++);
            }
        }

        orders[orders.Count - 1] = -1;
        tiles[orders.Count - 1].gameObject.SetActive(false);
    }

    void DrawTiles()
    {
        state = STATE.wait;

        Transform parent = new GameObject("Tiles").transform; // 화면의 타일을 저장할 컨테이너 생성

        // 타일 간격 구하기
        Sprite sprite = tiles[0].GetComponent<SpriteRenderer>().sprite; 
        tileSpan = sprite.bounds.size.x * tileScale; // 타일 간격

        for(int y = 0; y < sliceCnt; y++)
        {
            for(int x = 0; x < sliceCnt; x++)
            {
                int idx = y * sliceCnt + x;

                int n = orders[idx];
                if( n == -1)
                {
                    n = orders.Count - 1;
                }

                Vector3 pos = new Vector3(x * tileSpan - 3.9f, -y * tileSpan, 0); // 타일 위치 계산 식
                tiles[n].position = pos;
                tiles[n].parent = parent;
            }
        }

        state = STATE.idle;
    }
    
    void ShffulTile() // 섞기 위한 함수
    {    
        for(int i = 0; i < orders.Count - 1; i++)
        {
            int n = Random.Range(i + 1, orders.Count);
            int tmp = orders[i];
            orders[i] = orders[n];
            orders[n] = tmp;
        }
        if(!CheckValidate())
        {
            ShffulTile();
        }
    }

    bool CheckValidate()
    {
        int sum = 0;
        for(int i = 0; i < orders.Count -1; i++)
        {
            if(orders[i] == -1)
            {
                continue;
            }
            for(int j= i + 1; j < orders.Count; j++)
            {
                if(orders[j] != -1 && orders[i] > orders[j])
                {
                    sum++;
                }
            }
        }
        return (sum % 2 == 0);
    }

    void CheckTiles()
    {
        state = STATE.wait;

        dir = 0;
        moveTiles.Clear();

        int tile = orders.FindIndex(x => x == tileNum);
        int blank = orders.FindIndex(x => x == -1);

        int x1 = tile % sliceCnt;
        int y1 = tile / sliceCnt;

        int x2 = blank % sliceCnt;
        int y2 = blank / sliceCnt;

        if(x1 == x2)
        {
            moveTiles.Add(blank);

            dir = (y1 > y2) ? 1 : 3;
            int row = (y1 > y2) ? sliceCnt : -sliceCnt;
            int idx = blank + row;

            while (true)
            {
                moveTiles.Add(idx);
                idx += row;
                if((dir == 1 && idx > tile) || (dir == 3 && idx < tile))
                {
                    break;
                }
            }
        }
        else if(y1 == y2)
        {
            moveTiles.Add(blank);

            dir = (x1 > x2) ? 4 : 2;
            int col = (x1 > x2) ? 1 : -1;
            int idx = blank + col;

            while (true)
            {
                moveTiles.Add(idx);
                idx += col;
                if((dir == 2 && idx < tile) || (dir == 4 && idx > tile))
                {
                    break;
                }
            }
        }

        state = (moveTiles.Count > 0) ? STATE.move : STATE.idle;

        if(state == STATE.move)
        {
            moveCnt += moveTiles.Count - 1;
        }
    }

    void SetTouch (int _tileNum)
    {
        if(state == STATE.idle)
        {
            tileNum = _tileNum;
            state = STATE.touch;
        }

        Debug.Log("TileNum = " + tileNum);
    }
    void SetCalc()
    {
        state = STATE.calc;
    }

    void MoveTiles()
    {
        state = STATE.wait;

        Vector3[] vectors = { Vector3.zero, Vector3.up, Vector3.right, Vector3.down, Vector3.left };

        foreach (int idx in moveTiles)
        {
            int p = orders[idx];
            if(p == -1)
            {
                continue;
            }
            Vector3 pos = tiles[p].position;
            Vector3 target = pos + vectors[dir] * tileSpan;
            tiles[p].SendMessage("SetMove", target);
        }

        canCalc = true;

    }

    void CalcOrder()
    {
        if (!canCalc)
        {
            state = STATE.idle;
            return;
        }

        canCalc = false;
        state = STATE.wait;

        for(int i = 0;  i< moveTiles.Count - 1; i++)
        {
            int n1 = moveTiles[i];
            int n2 = moveTiles[i + 1];
            orders[n1] = orders[n2];
        }

        int blank = moveTiles[moveTiles.Count - 1];
        orders[blank] = -1;

        bool finished = true;
        for(int i = 0; i < orders.Count - 1; i++)
        {
            if(orders[i] != i)
            {
                finished = false;
                break;
            }
        }

        if (finished)
        {
            state = STATE.finish;
        }
        else
        {
            state = STATE.idle;
        }
    }

    void SetFinish()
    {
        foreach (Transform tile in tiles)
        {
            tile.GetComponent<SpriteRenderer>().material.SetInt("_count", 0);
        }

        int last = orders.Count - 1;
        tiles[last].gameObject.SetActive(true);
        tiles[last].position = tiles[last - 1].position + Vector3.right * tileSpan;

        panelComplete.SetActive(true);

    }
    void Awake()
    {
        InitGame();
        ShffulTile();
        DrawTiles();

        startTime = Time.time;

    }
    
    void Update()
    {
        switch (state)
        {
            case STATE.touch:
                CheckTiles();
                break;
            case STATE.move:
                MoveTiles();
                break;
            case STATE.calc:
                CalcOrder();
                break;
            case STATE.finish:
                SetFinish();
                break;

        }

        if (canUI) SetTime();
    }

    public void OnButtonClick(GameObject button)
    {
        switch (button.name)
        {
            case "BtnAgain":
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                break;
            case "BtnQuit":
                Application.Quit();
                break;
        }
    }

}
