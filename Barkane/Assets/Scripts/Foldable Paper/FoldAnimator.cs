using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoldAnimator : MonoBehaviour
{
    public float foldDuration = 0.25f;
    public bool isFolding = false;
    //public bool isChecking = false;
    public FoldablePaper foldablePaper;
    public GameObject SquareCollider;

    public int foldCount = 0;
    private int internalCount = 0; //C: ticks more often than foldCount, used for priority in rendering squares

    public LayerMask squareCollidingMask;

    public bool checkRaycast = false; //C: set to true when the rest of the check is good
    public bool raycastCheckDone = false;
    public FoldData foldData = new FoldData();
    public Coroutine checkCoroutine = null;
    public bool raycastCheckReturn = false;
    public bool crDone = false;

    private void Start() 
    {
        foldablePaper = FindObjectOfType<FoldablePaper>();
    }
    //C: Tries to fold the given objects. Returns true and folds if successful, returns false if this fold is not possible.
    public bool TryFold(PaperJoint foldJoint, FoldObjects foldObjects, Vector3 center, Vector3 axis, float degrees)
    {
        Debug.Log("trying to fold");
        //C: we need to wait until FixedUpdate to check the colliders. So we Call CCF, then if that passes, we know we've created collider data
        // that we need to call CheckColliders. If that passes, then it will call fold. 
        if(CheckCanFold(foldJoint, foldObjects, center, axis, degrees)) 
        {
            checkCoroutine = StartCoroutine(WaitForColliderCheck(foldJoint, foldObjects, center, axis, degrees));
            //AudioManager.Instance?.Play("Fold");
           // Fold(foldJoint, foldObjects, center, axis, degrees);
            return true;
        }
        else
        {
            AudioManager.Instance?.Play("Fold Error");
            //play error sound
        }
        return false;
        
    }

    private void Update() {
        if(crDone && checkCoroutine != null)
        {
            crDone = false;
            StopCoroutine(checkCoroutine);
            checkCoroutine = null;
        }
    }

    public IEnumerator WaitForColliderCheck(PaperJoint foldJoint, FoldObjects foldObjects, Vector3 center, Vector3 axis, float degrees)
    {
        Debug.Log("enter CR");
        yield return new WaitUntil(() => raycastCheckDone);
        Debug.Log("raycast done");
        raycastCheckDone = false;
        if(raycastCheckReturn){
            Fold(foldJoint, foldObjects, center, axis, degrees);
            raycastCheckReturn = false;
        }
        foldData = new FoldData();
        crDone = true;
    }

    public bool CheckCanFold(PaperJoint foldJoint, FoldObjects foldObjects, Vector3 center, Vector3 axis, float degrees)
    {
        if(isFolding) {
            Debug.Log("Cannot fold: You can't do 2 folds at once");
            return false;
        }
        if(checkCoroutine != null){
            Debug.Log("Cannot fold: currently checking colliders");
            return false;
        }
        //C: check selected joints to ensure straight line
        HashSet<int> x = new HashSet<int>();
        HashSet<int> y = new HashSet<int>();
        HashSet<int> z = new HashSet<int>();

        foreach(PaperJoint pj in foldablePaper.PaperJoints)
        {
            if(pj.showLine)
            {
                x.Add(Vector3Int.RoundToInt(pj.transform.position).x);
                y.Add(Vector3Int.RoundToInt(pj.transform.position).y);
                z.Add(Vector3Int.RoundToInt(pj.transform.position).z);
            }
        }

        if((x.Count > 1 && y.Count >1) || (x.Count > 1 && z.Count >1) || (z.Count > 1 && y.Count >1)) {
            Debug.Log($"Cannot fold: joint is kinked. {x.Count} {y.Count} {z.Count}");
            return false;
        }

        //C: Check that we aren't folding though a back to back square by getting vector of top and bottom in square stack and ensuring that 
        //the direction of that vector does not change 

        List<List<PaperSquare>> overlaps = foldablePaper.FindOverlappingSquares();
        foreach(List<PaperSquare> list in overlaps)
        {
            //Vector3 intial = Vector3.zero;
            //Vector3 newVec = Vector3.zero;
            if(list.Count > 1) //C: if count = 1 then only 1 square, can't fold through itself
            {
                GameObject parent = new GameObject();
                parent.transform.position = center;
                List<GameObject> activeSides = new List<GameObject>();
                GameObject t1 = new GameObject();
                GameObject t2 = new GameObject();
                foreach(PaperSquare ps in list) 
                {
                    if(ps.BottomHalf.activeSelf)
                        activeSides.Add(ps.BottomHalf);
                    if(ps.TopHalf.activeSelf)
                        activeSides.Add(ps.TopHalf);
                }
                Debug.Log($"AS length: {activeSides.Count}");
                foreach(GameObject go in activeSides)
                {
                    Debug.Log($"{go.GetComponentInParent<PaperSquare>().name} {go.name} is active");
                }
                t1.transform.position = activeSides[0].transform.position;
                t2.transform.position = activeSides[1].transform.position; 
                Vector3 midInit = new Vector3((t2.transform.position.x + t1.transform.position.x)/2,
                                            (t2.transform.position.y + t1.transform.position.y)/2,
                                            (t2.transform.position.z + t1.transform.position.z)/2);
                Vector3 intial = Vector3.Normalize(t2.transform.position - t1.transform.position);
                Debug.Log(intial);
                //reparent transforms and rotate about axis
                t1.transform.parent = parent.transform;
                t2.transform.parent = parent.transform;    
                parent.transform.RotateAround(center, axis, degrees);
                Vector3 mid = new Vector3((t2.transform.position.x + t1.transform.position.x)/2,
                                            (t2.transform.position.y + t1.transform.position.y)/2,
                                            (t2.transform.position.z + t1.transform.position.z)/2);
                Vector3 final = Vector3.Normalize(mid - midInit);
                Debug.Log(final);
                Destroy(parent);
                Debug.Log(Vector3.Angle(intial, final));
                if(Vector3.Angle(intial, final) > 90.0f){
                    Debug.Log("Cannot fold: would clip through adj paper");
                    return false;
                }
            }
        }

        FoldData fd = new FoldData();
        fd.axis = axis;
        fd.center = center;
        fd.degrees = degrees;
        fd.foldJoint = foldJoint;
        fd.foldObjects = foldObjects;

        //C: need to transfer data out to be used for raycast stuff
        foldData = fd;
        checkRaycast = true;
        return true;
        //C: The final check is for rotating into hitboxes (ie the player, other paper, obstacles, etc)
        //We duplicate the square hitboxes and check several points along the rotation;
        
       /* Debug.Log("check collision");
        int numChecks = 10;
        
        GameObject parent2 = new GameObject();
        List<GameObject> copiesList = new List<GameObject>();
        foreach(GameObject go in foldObjects.foldSquares)
        {
            GameObject newSquare = Instantiate(SquareCollider, go.transform.position, go.transform.rotation);
            newSquare.transform.parent = parent2.transform;
            copiesList.Add(newSquare);
        }
        
        //Ideally we should check every point along the rotation axis, but this is impracticle. 
        for(int i = 1; i <= numChecks; i++) {
            parent2.transform.RotateAround(center, axis, degrees/(numChecks+1));
            foreach(GameObject go in copiesList)
            {
                RaycastHit hit;
                bool collide = Physics.BoxCast(go.GetComponent<Collider>().bounds.center, transform.localScale, transform.forward, out hit, transform.rotation, 0, squareCollidingMask);
                if(collide){
                    Debug.Log($"Cannot Fold: hit {hit.transform.gameObject} when calculating fold path");
                    Destroy(parent2);
                    return false;
                }
            }
        }

        Debug.Log("end collision check");

        Destroy(parent2);


        //C: if we passed all these checks, then we can fold :)
        return true;*/
    }

    private void FixedUpdate() {
        if(checkRaycast)
            raycastCheckReturn = CheckRayCast();
    }

    private bool CheckRayCast() {
        Debug.Log("checking raycast...");
        checkRaycast = false;
        int numChecks = 10;
        
        GameObject parent2 = new GameObject();
        parent2.transform.position = foldData.center;
        List<GameObject> copiesList = new List<GameObject>();
        foreach(GameObject go in foldData.foldObjects.foldSquares)
        {
            GameObject newSquare = Instantiate(SquareCollider, go.transform.position, go.transform.rotation);
            newSquare.transform.parent = parent2.transform;
            copiesList.Add(newSquare);
        }
        
        //Ideally we should check every point along the rotation axis, but this is not feasible. 
        for(int i = 1; i <= numChecks; i++) {
            parent2.transform.RotateAround(foldData.center, foldData.axis, foldData.degrees/(numChecks+1));
            foreach(GameObject go in copiesList)
            {
              //  Debug.Log("collision check " + i);
                RaycastHit hit;
                bool collide = Physics.Raycast(go.transform.position, go.transform.up, out hit, 0.1f, squareCollidingMask);
               // bool collide = Physics.BoxCast(go.transform.position, new Vector3(0.45f, 0.01f, 0.45f), go.transform.up, out hit, transform.rotation, 0.1f, squareCollidingMask);
                Debug.DrawRay(go.transform.position, go.transform.up * 0.1f, Color.red, 100);
                if(collide){
                    Debug.Log($"Cannot Fold: hit {hit.transform.gameObject.name} when calculating fold path");
                    //Destroy(parent2);
                    Debug.DrawRay(go.transform.position, hit.transform.position - go.transform.position, Color.green, 100);
                    raycastCheckDone = true;
                    return false;
                }
            }
        }

        Debug.Log("end collision check");

        //Destroy(parent2);
        raycastCheckDone = true;
        return true;
    }

    //C: folds the given list of squares along the given line by the given number of degrees
    public void Fold(PaperJoint foldJoint, FoldObjects foldObjects, Vector3 center, Vector3 axis, float degrees)
    {
        if(!isFolding) 
        {
            var foldJointRenderer = foldJoint.JointRenderer;
            if(foldJointRenderer != null)
                StartCoroutine(FoldHelper(foldObjects, center, axis, degrees, foldJointRenderer.DisableMeshAction, foldJointRenderer.EnableMeshAction));
            else
                StartCoroutine(FoldHelper(foldObjects, center, axis, degrees));
        }
            
    }

    
    private IEnumerator FoldHelper(FoldObjects objectsToFold, Vector3 center, Vector3 axis, float degrees, System.Action beforeFold = null, System.Action afterFold = null)
    {
        AudioManager.Instance?.Play("Fold");
        isFolding = true;
        GameObject tempObj = new GameObject(); //used for reparenting/rotating
        GameObject target = new GameObject(); //used for setting correct position due to float jank
        tempObj.transform.position = center;
        target.transform.position = center;
       
        foreach(GameObject o in objectsToFold.foldSquares)
        {
            o.transform.parent = tempObj.transform;
        }
        
        foreach(GameObject o in objectsToFold.foldJoints)
        {
            o.transform.parent = tempObj.transform;
            o.GetComponent<PaperJoint>().ToggleCollider(false);
        }

        if(beforeFold != null)
            beforeFold();

        StoreAllSquarePos();
        float t = 0;
        int wait = 1;
        while (t < foldDuration)
        {
            t += Time.deltaTime;
            tempObj.transform.RotateAround(center, axis, (degrees / foldDuration) * Time.deltaTime);
            wait--;
            if(wait == 0){
                UpdateSquareVisibility(objectsToFold);
            }
            yield return null;
        }
        target.transform.RotateAround(center, axis, degrees);
        tempObj.transform.SetPositionAndRotation(target.transform.position, target.transform.rotation);

        foreach(GameObject o in objectsToFold.foldSquares)
        {
            o.transform.position = Vector3Int.RoundToInt(o.transform.position);
            o.transform.parent =  objectsToFold.squareParent;
        }

        foreach(GameObject o in objectsToFold.foldJoints)
        {
            o.transform.position = Vector3Int.RoundToInt(o.transform.position);
            o.transform.parent =  objectsToFold.jointParent;
            o.GetComponent<PaperJoint>().ToggleCollider(true);
        }
        Destroy(tempObj);
        Destroy(target);
        isFolding = false;

        UpdateSquareVisibility(objectsToFold);

        if(afterFold != null)
             afterFold();
        UIManager.UpdateFoldCount(++foldCount);
    }
    private void StoreAllSquarePos()
    {
        foreach(PaperSquare ps in foldablePaper.PaperSquares)
        {
            ps.StorePosition(ps.transform.position);
        }
    }

    private void UpdateSquareVisibility(FoldObjects foldObjects)
    {
        List<List<PaperSquare>> overlaps = foldablePaper.FindOverlappingSquares();

        //C: at the start of a fold, we are looking to re-enable sides if the adj square has been removed;
        //if(foldStart)
        //{
            foreach(List<PaperSquare> list in overlaps)
            {
                foreach(PaperSquare ps in list) 
                {
                    ps.CheckAndRemoveRefs(list);
                }
            }
       //}

        //C: at the end of a fold, we check positions to see if new squares have been added. If they have
        //else
        //{
            foreach(List<PaperSquare> list in overlaps)
            {
                if(list.Count == 1) //C: only 1 square, enable both meshes
                {
                    list[0].topStack = null;
                    list[0].bottomStack = null;
                    list[0].ToggleBottom(true);
                    list[0].ToggleTop(true);
                }
                else
                {
                    Debug.Log($"{list.Count} squares at location {list[0].transform.position}");
                    //We arbitrarily pick one side of the first square to be the "top", which is then used as a comparison for other square's top/bottoms
                    Vector3 topHalfNorm = list[0].TopHalf.transform.up;
                    
                    HashSet<GameObject> activeSides = new HashSet<GameObject>();
                    HashSet<GameObject> activeFoldSides = new HashSet<GameObject>();

                    GameObject foldTop = null;
                    GameObject foldBot = null;
                    GameObject stationaryTop = null;
                    GameObject stationaryBot = null;

                    Vector3 prevPos = Vector3.zero;
                    //C: there should be a total of 2 sides enabled in each list currently. We need to figure out which of them to now hide.
                    foreach(PaperSquare ps in list) 
                    {
                        activeSides.UnionWith(ps.GetOpenSides(!CoordUtils.ApproxSameVector(topHalfNorm, ps.TopHalf.transform.up)));
                        if(foldObjects.foldSquares.Contains(ps.gameObject))
                        {
                         //   Debug.Log($"{ps.gameObject.name} is in fold list on stack of size {list.Count}");
                            activeFoldSides.UnionWith(ps.GetOpenSides(!CoordUtils.ApproxSameVector(topHalfNorm, ps.TopHalf.transform.up)));
                            prevPos = ps.storedPos;
                        }
                    }
                    
                    Debug.Log("fold side " + activeFoldSides.Count);
                    Debug.Log("total " + activeSides.Count);
                    foreach(GameObject go in activeSides)
                    {
                        if(activeFoldSides.Contains(go))
                        {
                            //Debug.Log(go.GetComponentInParent<PaperSquare>().gameObject.name + " " + go.name + " is in active fold sides");
                            if(CoordUtils.ApproxSameVector(topHalfNorm, go.transform.up))
                            {
                                foldTop = go;
                              //  Debug.Log("fold top is " + go.GetComponentInParent<PaperSquare>().gameObject.name + " " + go.name);
                            }
                            else
                            {
                                foldBot = go;
                             //   Debug.Log("fold bot is " + go.GetComponentInParent<PaperSquare>().gameObject.name + " " + go.name);
                            }
                        }
                        else
                        {
                            if(CoordUtils.ApproxSameVector(topHalfNorm, go.transform.up))
                            {
                                stationaryTop = go;
                               // Debug.Log("stat top is " + go.GetComponentInParent<PaperSquare>().gameObject.name + " " + go.name);
                            }
                            else
                            {
                                stationaryBot = go;
                               // Debug.Log("stat bot  is " + go.GetComponentInParent<PaperSquare>().gameObject.name + " " + go.name);
                            }
                        }
                    }
                    if(foldTop == null)
                        Debug.Log("fold top is null");
                    if(foldBot == null)
                        Debug.Log("fold bot is null");
                      if(stationaryTop == null)
                        Debug.Log("stat top is null");
                    if(stationaryBot == null)
                       Debug.Log("stat bot is null");
//                    Debug.Log(foldTop.name + " " + foldBot.name + " " + stationaryTop.name + " " + stationaryBot.name);
                        //Check to see if we should like S top and F bot or F top and S bot

                    if(foldTop != null && foldBot != null && stationaryTop != null && stationaryBot != null)
                    {
                    float topDist = Vector3.Magnitude(prevPos - stationaryTop.transform.position);
                    float botDist = Vector3.Magnitude(prevPos - stationaryBot.transform.position);
                    if(topDist == botDist) Debug.Log("same dist");
                    //foldTop.GetComponentInParent<PaperSquare>().invertForGetOpen = !CoordUtils.ApproxSameVector(foldTop.transform.up, foldTop.GetComponentInParent<PaperSquare>().TopHalf.transform.up);
                    if(topDist > botDist) //new square is on the bottom, join top of fold and bottom of stationary
                    {
                        Debug.Log("top side");
                        foldTop.SetActive(false);
                        stationaryBot.SetActive(false);
                        PaperSquare foldSquare = foldTop.GetComponentInParent<PaperSquare>();
                        PaperSquare statSquare = stationaryBot.GetComponentInParent<PaperSquare>();
                        foldSquare.bottomStack = statSquare;
                        statSquare.topStack = foldSquare;
                        foldSquare.UpdateHitboxes();
                        statSquare.UpdateHitboxes();
                    }
                    else //new fold square is on the top, join bot of fold and top of stationary
                    {
                        Debug.Log("bottom side");
                        foldBot.SetActive(false);
                        stationaryTop.SetActive(false);
                        PaperSquare foldSquare = foldBot.GetComponentInParent<PaperSquare>();
                        PaperSquare statSquare = stationaryTop.GetComponentInParent<PaperSquare>();
                        foldSquare.topStack = statSquare;
                        statSquare.bottomStack = foldSquare;
                        foldSquare.UpdateHitboxes();
                        statSquare.UpdateHitboxes();
                    }
                    }
                }
            }
        //}


        //update priority
        /*foldObjects.UpdateSquarePriority(++internalCount);
        List<List<PaperSquare>> overlaps = foldablePaper.FindOverlappingSquares();
        foreach(List<PaperSquare> list in overlaps)
        {
            if(list.Count == 1) //C: only 1 square, enable both meshes
            {
                list[0].ToggleBottom(true);
                list[0].ToggleTop(true);
                //list[0].topSide.ToggleMesh(true);
                //list[0].bottomSide.ToggleMesh(true);
            }
            else //C: otherwise we need to find 2 highest priority squares and see which of their meshes should be active, disable all other meshes.
            {
                foreach(PaperSquare ps in list)    
                {
                    //if(ps.topStack) == null;
                }
               /* list.Sort();
                for(int i = 0; i < list.Count - 2; i++){
                    list[i].ToggleBottom(false);
                    list[i].ToggleTop(false);
                }
                PaperSquare s1 = list[list.Count - 1];
                PaperSquare s2 = list[list.Count - 2];

            //C: When we have the 2 highest priority squares, we can group the faces in the same direction together, then check the priority of those 
            //faces that are in the same direction to determine which to display
            //We arbitrarily pick one side of the first square to be the "top", then add to this list based on the normals of the other square's top/bottoms
                List<SquareSide> topHalfList = new List<SquareSide>();
                List<SquareSide> botHalfList = new List<SquareSide>();
                Vector3 topHalfNorm = s1.TopHalf.transform.up;
                foreach (PaperSquare square in list){
                    if(CoordUtils.ApproxSameVector(topHalfNorm, square.TopHalf.transform.up))
                    {
                        topHalfList.Add(square.topSide);
                        botHalfList.Add(square.bottomSide);
                    }
                    else
                    {
                        botHalfList.Add(square.topSide);
                        topHalfList.Add(square.bottomSide);
                    }*/
               // }
            
            //We now have a list of 2 "top" faces and 2 "bottom" faces. We now want to check if we should enable the top of s1 and the bottom of s2 or 
            //vice versa.

           // }





           /* Debug.Log("Overlapping Squares");
            //in each list of overlaps, we need to calculate the highest priorty top square and highest priority bottom square, then hide everything else
            List<SquareSide> topHalfList = new List<SquareSide>();
            List<SquareSide> botHalfList = new List<SquareSide>();

            //C: We arbitrarily pick one side of the first square to be the "top", then add to this list based on the normals of the other squares top/bottoms
            PaperSquare square1 = list[0];
            //Vector3 topHalfNorm = square1.TopHalf.transform.up;
            //Vector3 botHalfNorm = square1.BottomHalf.transform.up;
            foreach (PaperSquare square in list){
                if(CoordUtils.ApproxSameVector(topHalfNorm, square.TopHalf.transform.up))
                {
                    topHalfList.Add(square.topSide);
                    botHalfList.Add(square.bottomSide);
                }
                else
                {
                    botHalfList.Add(square.topSide);
                    topHalfList.Add(square.bottomSide);
                }
                //Vector3 topHalfNorm = square.TopHalf.transform.up;
                //Vector3 botHalfNorm = square.BottomHalf.transform.up;
                //Debug.Log($"{square.gameObject.name} top vector {topHalfNorm} bottom vector {botHalfNorm}");
           }

            topHalfList.Sort();
            botHalfList.Sort();
            Debug.Log(topHalfList);
            Debug.Log(botHalfList);
            for (int i = 0; i < topHalfList.Count; i++)
                topHalfList[i].ToggleMesh(i == topHalfList.Count - 1);
            for (int i = 0; i < botHalfList.Count; i++)
                botHalfList[i].ToggleMesh(i == botHalfList.Count - 1);*/
    }
}

//C: we should pass this insead of a bunch of params but i have 90 min to make this game work aaaaa
public class FoldData 
{
    public PaperJoint foldJoint;
    public FoldObjects foldObjects;
    public Vector3 center;
    public Vector3 axis;
    public float degrees;
}

