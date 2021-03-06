﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class Spline {

    public List<ControlPoint> points;
    public string name;
    [SerializeField]
    private SplineSettings settings;
    public bool[] assetIsActive;        //Bools to set assets in the settings on or of for this spline

    private float[] arcLengthTable;     //Contains a list of lengths between parametric values of the bezier curves. It is used to get a point at a distance along the spline.
    private static int tableSize = 100; //Precision value for the table

    //Constructor
    public Spline() {
        points = new List<ControlPoint> {
            new ControlPoint(Vector3.forward, Vector3.forward),
            new ControlPoint(Vector3.forward * 2, Vector3.forward)
        };
        ResetArcLengthTable();
        name = string.Concat("Spline");
        settings = null;
        assetIsActive = null;
    }

    //Constructor using position and index in component
    public Spline(Vector3 position, int index) {
        points = new List<ControlPoint> {
            new ControlPoint(position, Vector3.forward),
            new ControlPoint(position + Vector3.forward, Vector3.forward)
        };
        ResetArcLengthTable();
        name = string.Concat("Spline_", index.ToString("D2"));
        settings = null;
        assetIsActive = null;
    }

    //Add a new controlpoint in front of the spline with the same direction as the last point
    public void AddControlPoint () {
        points.Add(new ControlPoint(
            points[points.Count - 1].GetAnchorPosition() + points[points.Count - 1].GetRelativeHandlePosition(1).normalized, //Position
            .5f * points[points.Count - 1].GetRelativeHandlePosition(1).normalized));                                        //Direction
        ResetArcLengthTable();
    }

    public void RemoveControlPoint (ControlPoint point) {
        points.Remove(point);
        ResetArcLengthTable();
    }

    //Insert a point between two other points, or before the first point
    public void InsertControlPoint (int index) {
        Vector3 newAnchor = new Vector3();
        Vector3 newDirection = new Vector3();
        if (index == 0) {
            newAnchor = points[index].GetAnchorPosition() + points[index].GetRelativeHandlePosition(0).normalized;
            newDirection = 5f * points[index].GetRelativeHandlePosition(1).normalized;
        } else {
            newAnchor = GetPoint(index - 1, .5f);
            newDirection = GetDirection(index - 1, .5f) * points[index].GetRelativeHandlePosition(0).magnitude * .5f;
            points[index - 1].SetMode(BezierControlPointMode.Aligned);
            points[index - 1].SetRelativeHandlePosition(1, points[index - 1].GetRelativeHandlePosition(1) * .5f);
            points[index].SetMode(BezierControlPointMode.Aligned);
            points[index].SetRelativeHandlePosition(0, points[index].GetRelativeHandlePosition(0) * .5f);
        }
        points.Insert(index, new ControlPoint(newAnchor, newDirection));
        ResetArcLengthTable();
    }

    //Get position on spline at the arcdistance of the entire spline
    public Vector3 GetPoint(float t) {
        t = GetArcPos(t);

        int curve = (int)t;
        t = t % 1;
        if (curve == points.Count - 1) {
            curve = points.Count - 2;
            t = 1;
        }

        return GetPoint(curve, t);
    }

    //Get direction on spline at the arcdistance of the entire spline
    public Vector3 GetDirection(float t) {
        t = GetArcPos(t);

        int curve = (int)t;
        t = t % 1;
        if (curve == points.Count - 1) {
            curve = points.Count - 2;
            t = 1;
        }
        return GetDirection(curve, t);
    }

    //Get up vector on spline at the arcdistance of the entire spline
    public Vector3 GetUp(float t) {
        t = GetArcPos(t);

        int curve = (int)t;
        t = t % 1;
        if (curve == points.Count - 1) {
            curve = points.Count - 2;
            t = 1;
        }
        Vector3 direction = GetDirection(curve, t);
        Quaternion rotation = Quaternion.Lerp(points[curve].GetRotation(), points[curve + 1].GetRotation(), t);
        return Vector3.ProjectOnPlane(rotation * Vector3.up, direction);
    }

    //Returns the parametric value for the position at arcdistance t
    private float GetArcPos (float t) {
        for (int i = 0; i < arcLengthTable.Length; i++) {
            if (arcLengthTable[i] > t) {
                float T1 = (float)(i - 1) / (float)tableSize;
                float T2 = (float)(i) / (float)tableSize;
                float dT = (t - arcLengthTable[i - 1]) / (arcLengthTable[i] - arcLengthTable[i - 1]);
                return Mathf.Lerp(T1, T2, dT);
            }
        }
        return points.Count - 1;
    }

    //Get the position for a bezier curve at position t
    private Vector3 GetPoint(int curve, float t) {
        return Bezier.GetPoint(points[curve].GetAnchorPosition(), points[curve].GetHandlePosition(1),
            points[curve + 1].GetHandlePosition(0), points[curve + 1].GetAnchorPosition(), t);
    }

    //Get the direction for a bezier curve at position t
    private Vector3 GetDirection(int curve, float t) {
        return Bezier.GetFirstDerivative(points[curve].GetAnchorPosition(), points[curve].GetHandlePosition(1),
            points[curve + 1].GetHandlePosition(0), points[curve + 1].GetAnchorPosition(), t);
    }

    //Returns the entire length of this spline
    public float GetArcLength() {
        return arcLengthTable[arcLengthTable.Length - 1];
    }

    //Recaclculates the arcLengthTable
    public void ResetArcLengthTable () {
        arcLengthTable = new float[(points.Count - 1) * tableSize + 1];
        arcLengthTable[0] = 0f;
        Vector3 lastPos = points[0].GetAnchorPosition();
        for (int i = 0; i < points.Count - 1; i++) {
            for (int j = 0; j < tableSize; j++) {
                if (i + j != 0) {
                    Vector3 nextPos = Bezier.GetPoint(points[i].GetAnchorPosition(), points[i].GetHandlePosition(1), points[i + 1].GetHandlePosition(0), points[i + 1].GetAnchorPosition(), (float)j / (float)tableSize);
                    arcLengthTable[i * tableSize + j] = arcLengthTable[i * tableSize + j - 1] + (nextPos - lastPos).magnitude;
                    lastPos = nextPos;
                }
            }
        }
        arcLengthTable[arcLengthTable.Length - 1] = arcLengthTable[arcLengthTable.Length - 2] + (points[points.Count - 1].GetAnchorPosition() - lastPos).magnitude;
    }

    public void SetSettings(SplineSettings _settings) {
        settings = _settings;
        assetIsActive = new bool[settings.generated.Count + settings.placers.Count];
        for (int i = 0; i < assetIsActive.Length; i++) {
            assetIsActive[i] = true;
        }
    }

    public SplineSettings GetSettings() {
        return settings;
    }

    /* Static function to calculate the euler angles for a point given its up and forward vector.
     * This is similar to Quaternion.LookRotation. It calculates x, y and z in a different order that makes more sense for a spline*/
    public static Vector3 GetEulerAngles(Vector3 up, Vector3 forward) {
        Vector3 euler = new Vector3();
        euler.y = Mathf.Rad2Deg * Mathf.Atan2(forward.x, forward.z);

        Vector3 xzDirection = forward;
        xzDirection.y = 0;
        euler.x = -Mathf.Rad2Deg * Mathf.Atan2(forward.y, xzDirection.magnitude);

        Vector3 perpendicular = Vector3.Cross(Vector3.up, xzDirection);
        Vector3 normal = Vector3.Cross(forward, perpendicular);
        if (Vector3.Angle(perpendicular, up) < 90)
            euler.z = 360 - Vector3.Angle(normal, up);
        else
            euler.z = Vector3.Angle(normal, up);

        return euler;
    }
}
