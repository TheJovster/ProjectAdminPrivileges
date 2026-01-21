using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAdminPrivileges.Utility 
{
    public static class Interpolation 
    {
        public static float Lerp(float curremt, float target, float speed, float deltaTime) 
        {
            return Mathf.Lerp(curremt, target, speed * deltaTime);
        }

        public static Vector2 Lerp(Vector2 curremt, Vector2 target, float speed, float deltaTime) 
        {
            return Vector2.Lerp(curremt, target, speed * deltaTime);
        }

        public static Vector3 Lerp(Vector3 curremt, Vector3 target, float speed, float deltaTime) 
        {
            return Vector3.Lerp(curremt, target, speed * deltaTime);
        }

        public static float EaseInOut(float time) 
        {
            return time * time * (3f - 2f * time);
        }

        public static float EaseOut(float time) 
        {
            return 1f - (1f - time) * (1f - time);
        }

    }
}
