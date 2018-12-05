﻿using LunarLabs.Parser;
using LunarLabs.WebServer.Core;
using System;
using System.Collections;

namespace LunarLabs.WebServer.Templates
{
    internal class TemplateException: Exception
    {
        public TemplateException(string msg): base(msg)
        {

        }
    }

    public class PathRenderingKey : RenderingKey
    {
        private string key;
        private string[] steps;

        public override RenderingType RenderingType => RenderingType.Any;

        public override string ToString()
        {
            return key;
        }

        internal PathRenderingKey(string key)
        {
            this.key = key;
            this.steps = key.Split( '.' );
        }

        public override object Evaluate(RenderingContext context)
        {
            object obj = null;
            int stackPointer = context.DataStack.Count - 1;

            if (steps != null)
            {
                // NOTE this while is required for support access to out of scope variables 
                while (stackPointer >= 0)
                {
                    obj = context.DataStack[stackPointer];

                    try
                    {
                        for (int i = 0; i < steps.Length; i++)
                        {
                            Type type = obj.GetType();
                            var key = steps[i];

                            if (type == typeof(DataNode))
                            {
                                var node = obj as DataNode;
                                if (node.HasNode(key))
                                {
                                    var val = node.GetNode(key);
                                    if (val != null)
                                    {
                                        if (val.ChildCount > 0)
                                        {
                                            obj = val;
                                        }
                                        else
                                        {
                                            obj = val.Value;
                                        }

                                        continue;
                                    }
                                }

                                if (stackPointer > 0)
                                {
                                    throw new TemplateException("node key not found: "+ this.key);
                                }
                                else
                                {
                                    return null;
                                }
                            }

                            var field = type.GetField(key);
                            if (field != null)
                            {
                                obj = field.GetValue(obj);
                                continue;
                            }

                            var prop = type.GetProperty(key);
                            if (prop != null)
                            {
                                obj = prop.GetValue(obj);
                                continue;
                            }

                            if (key.Equals("count"))
                            {
                                var collection = obj as ICollection;
                                if (collection != null)
                                {
                                    obj = collection.Count;
                                    continue;
                                }

                                throw new TemplateException("count key not found: " + this.key);
                            }

                            var dict = obj as IDictionary;
                            if (dict != null)
                            {
                                if (dict.Contains(key))
                                {
                                    obj = dict[key];
                                    continue;
                                }

                                type = obj.GetType();
                                Type valueType = type.GetGenericArguments()[1];
                                obj = valueType.GetDefault();
                                
                                if (obj == null && i < steps.Length -1)
                                {
                                    //throw new TemplateException("key not found: " + this.key);
                                    return null;
                                }

                                continue;
                            }

                            throw new TemplateException("key not found: " + this.key);
                        }

                    }
                    catch (TemplateException e)
                    {
                        // if an eval exception was thrown, try searching in the parent scope
                        stackPointer--;
                        if (stackPointer < 0)
                        {
                            throw e;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    break;
                }
            }

            return obj;
        }
    }
}
