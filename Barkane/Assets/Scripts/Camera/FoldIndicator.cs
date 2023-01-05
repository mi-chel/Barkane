using BarkaneEditor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//Transparent outline used to show the position of a fold
public class FoldIndicator : MonoBehaviour
{
    [SerializeField] private GameObject ghostSquarePrefab;
    public Vector2 foldCenter; //fold center in screen space
    private List<Transform> transforms = new List<Transform>();
    new private Camera camera;
    public FoldFailureType foldFailureType;


    private void Update() 
    {
        if(camera == null) return;
        foldCenter = camera.WorldToScreenPoint(CoordUtils.CalculateCenterTransform(transforms));
    }
    
    public void BuildIndicator(FoldData fd, Camera c)
    {
        Transform center = fd.foldObjects.foldJoints[0].transform;
        transform.position = center.position;
        foreach(GameObject go in fd.foldObjects.foldSquares)
        {
            GameObject newSquare = Instantiate(ghostSquarePrefab, go.transform.position, go.transform.rotation);
            var ghostSquareRenderer = newSquare.GetComponent<MeshRenderer>();
            if (ghostSquareRenderer)
            {
                //if(fail == FoldFailureType.NONE)
                    ghostSquareRenderer.sharedMaterial.SetColor("_Color", VFXManager.Theme.Silhouette);
               // else
                    ghostSquareRenderer.sharedMaterial.SetColor("_Color", Color.gray);
            }
            newSquare.transform.parent = gameObject.transform;
        }
        transform.RotateAround(center.position, center.rotation * Vector3.right, fd.degrees);
        int children = transform.childCount;
        transforms.Clear();
        for (int i = 0; i < children; i++)
            transforms.Add(transform.GetChild(i));
        
        camera = c;
        //foldFailureType = fail;
        foldCenter = camera.WorldToScreenPoint(CoordUtils.CalculateCenterTransform(transforms));
    }

}
