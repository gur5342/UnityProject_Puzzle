using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    float speed = 30;
    bool canMove = false;
    Vector3 target;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (canMove) {
            MoveTile();
        }
       
    }
    void MoveTile()
    {
        Vector3 pos = transform.position;
        pos = Vector3.MoveTowards(pos, target, speed * Time.deltaTime);
        transform.position = pos;

        if (Vector3.Distance(pos, target) < 0.05f)
        {
            transform.position = target;

            GameObject.Find("GameManager").SendMessage("SetCalc");
            canMove = false;
        }
    }

    void SetMove(Vector3 _target)
    {
        target = _target;
        canMove = true;
    }
    void OnMouseDown()
    {
        int n = int.Parse(name.Substring(4));
        GameObject.Find("GameManager").SendMessage("SetTouch", n);
    }
}
