using UnityEngine;

[System.Serializable]
public class JointData: FoldableObject
{
    public PaperJoint paperJoint;
    public Vector3 currentOffset;
    public Vector3 targetOffset;

    
    public JointData(PaperJoint paperJoint)
    {
        PositionData positionData = new(
            Vector3Int.RoundToInt(paperJoint.transform.position),
            paperJoint.transform.rotation,
            GetAxisFromCoordinates( Vector3Int.RoundToInt(paperJoint.transform.position))
        );
        currentPosition = positionData;
        targetPosition = positionData;
        this.paperJoint = paperJoint;
        storedParent = paperJoint.transform.parent;
    }

    public JointData(PositionData position, PaperJoint paperJoint)
    {
        currentPosition = position;
        targetPosition = position;
        this.paperJoint = paperJoint;
        storedParent = paperJoint.transform.parent;

    }

    public JointData(PositionData currentPosition, PositionData targetPosition, PaperJoint paperJoint)
    {
        this.currentPosition = currentPosition;
        this.targetPosition = targetPosition;
        this.paperJoint = paperJoint;
        storedParent = paperJoint.transform.parent;
    }

    public static Vector3 GetAxisFromCoordinates(Vector3Int coordinates)
    {
        if(coordinates.x % 2 == 0) return Vector3.right;
        if(coordinates.y % 2 == 0) return Vector3.up;
        return Vector3.forward;
    }

    public override void SendToTarget()
    {
        currentPosition = targetPosition;
        currentOffset = targetOffset;
        paperJoint.transform.position = currentPosition.location;
        paperJoint.transform.rotation = currentPosition.rotation;
        //paperJoint.Offset = currentYOffset;
    }

    public override void SetParent(Transform parent)
    {
        if(parent != null)
        {
            paperJoint.transform.parent = parent;
        }
        else
        {
            paperJoint.transform.parent = storedParent;
        }
    }
}


// public class JointData: FoldableObject
// {
//     public PaperJoint paperJoint;

//     public JointData(PaperJoint pj)
//     {
//         paperJoint = pj;
//         currLocation = Vector3Int.RoundToInt(pj.transform.position);
//         targetLocation = Vector3Int.RoundToInt(pj.transform.position);
//     }

//     public override void SendToTarget(Vector3 axis)
//     {
//         if(currLocation != targetLocation)
//         {
//             paperJoint.transform.position = targetLocation;
//             paperJoint.transform.Rotate(axis, 90, Space.World);
// //            Debug.Log("joint " + paperJoint.gameObject.name + " moved " + axis);
//         }
//         currLocation = targetLocation;
//         orientation = targetorientation;
//     }

//     public void SetCurrent(Vector3Int v)
//     {
//         currLocation = v;
//         orientation = GetAxisFromCoordinates(v);
//     }

//     public static Vector3 GetAxisFromCoordinates(Vector3Int coordinates)
//     {
//         if(coordinates.x % 2 == 0) return Vector3.right;
//         if(coordinates.y % 2 == 0) return Vector3.up;
//         return Vector3.forward;

//     }
// }


// public class SquareStack : FoldableObject
// {
    
//     public LinkedList<PaperSquare> squares = new();

//     public SquareStack(Vector3Int location)
//     {
//         SetCurrent(location);
//         targetLocation = location;
//         targetorientation = orientation;
//         //SetTarget(location);
//     }

//     // public Vector3 AxisVector {
//     //     get 
//     //     {
//     //         if(orientation == Axis.X) return Vector3.right;
//     //         if(orientation == Axis.Y) return Vector3.up;
//     //         return Vector3.forward;
//     //     }
//     // }

//     public Quaternion IndicatorRotation(Vector3 a) {
//             if(a == Vector3.right || a == Vector3.left) return Quaternion.Euler(0, 0, 90);
//             if(a == Vector3.up || a == Vector3.down) return quaternion.identity; 
//             return Quaternion.Euler(90, 0, 0);
//     }

//     public void SetCurrent(Vector3Int v)
//     {
//         currLocation = v;
//         orientation = GetAxisFromCoordinates(v);
//     }


//     // public void SetTargetAsCurrent()
//     // {
//     //     SetTarget(currLocation);
//     // }
    
//     public void SetTarget(Vector3Int target, Vector3 axis)
//     {
//         targetLocation = target;
//         targetorientation = GetTargetOrientation(axis);
//     }

//     public Vector3 GetTargetOrientation(Vector3 axis)
//     {
//         Vector3 result = Vector3.Cross(axis, orientation);
//         return result.magnitude == 0 ? orientation : result;
//     }
    
//     public Vector3 GetAxisFromCoordinates(Vector3Int coordinates)
//     {
//         if(coordinates.x % 2 != 0) return Vector3.right;
//         if(coordinates.y % 2 != 0) return Vector3.up;
//         return Vector3.forward;

//     }

//     public StackOverlapType GetOverlap(SquareStack other)
//     {
//         if(other == this) return StackOverlapType.SAME;
//         bool sameStart = currLocation == other.currLocation;
//         bool sameEnd = targetLocation == other.targetLocation;
//         if(sameStart && sameEnd) return StackOverlapType.BOTH;
//         if(sameEnd) return StackOverlapType.END; //Merge stacks at end of fold
//         if(sameStart) return StackOverlapType.START; //Split stacks at start of fold
//         return StackOverlapType.NONE;
//     }

//     public override void SendToTarget(Vector3 axis)
//     {
        
//         foreach(PaperSquare s in squares)
//         {
//             if(targetLocation != currLocation)
//             {
//             s.transform.position = targetLocation;
//             s.transform.Rotate(axis, 90);
//             }
//         }   
//         currLocation = targetLocation;
//         orientation = targetorientation;

//     }

//     private Vector3 GetRotationFromAxis(Vector3 axis)
//     {
//         if(axis == Vector3.right) return new Vector3(0, 0, 90);
//         if(axis == Vector3.left) return new Vector3(0, 0, -90);
//         if(axis == Vector3.down) return new Vector3(0, 0, -90);
//         if(axis == Vector3.forward) return new Vector3(-90, 0, 0);
//         if(axis == Vector3.back) return new Vector3(90, 0, 0);
//         return Vector3.zero;
//     }
// }
