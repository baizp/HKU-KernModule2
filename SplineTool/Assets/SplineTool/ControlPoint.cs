﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public enum BezierControlPointMode {
    Free,
    Aligned,
    Mirrored
}

[Serializable]
public class ControlPoint {
    [SerializeField]
    private Vector3 anchor;
    [SerializeField]
    private Vector3[] handles;
    public BezierControlPointMode mode;
    public int connectedIndex;

    //Constructor with position
    public ControlPoint(Vector3 position, Vector3 forward) {
        anchor = position;
        handles = new Vector3[2] {
            -.5f * forward,
            .5f * forward
        };
        mode = BezierControlPointMode.Mirrored;
        connectedIndex = -1;
    }

    //Get position
    public Vector3 GetAnchorPosition () {
        return anchor;
    }
    
    //Get handle position
    public Vector3 GetHandlePosition(int index) {
        return anchor + handles[index];
    }

    //Get relative handle position
    public Vector3 GetRelativeHandlePosition(int index) {
        return handles[index];
    }

    //Set position
    public void setAnchorPosition(Vector3 position) {
        anchor = position;
    }

    //Set handleposition
    public void setHandlePosition(int index, Vector3 position) {
        handles[index] = position - anchor;
        switch (mode) {
            case BezierControlPointMode.Free:
                break;
            case BezierControlPointMode.Aligned:
                Vector3 direction = anchor - position;
                handles[1 - index] = direction.normalized * handles[1 - index].magnitude;
                break;
            case BezierControlPointMode.Mirrored:
                handles[1 - index] = anchor - position;
                break;
        }
    }

    public Quaternion GetHandleRotation () {
        return Quaternion.FromToRotation(Vector3.forward, handles[1]);
    }

    public void SetHandleRotation (Quaternion rotation) {
        handles[0] = rotation * Vector3.back * handles[0].magnitude;
        handles[1] = rotation* Vector3.forward * handles[1].magnitude;
    }

    public float GetHandleScale () {
        return handles[1].magnitude;
    }

    public void SetHandleScale (float scale) {
        handles[0] = handles[0].normalized * scale;
        handles[1] = handles[1].normalized * scale;
    }
}
