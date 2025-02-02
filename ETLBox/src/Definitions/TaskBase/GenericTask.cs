﻿using ALE.ETLBox.ConnectionManager;
using ALE.ETLBox.Helper;
using CF = ALE.ETLBox.ControlFlow;
using System;
using ALE.ETLBox.Logging;

namespace ALE.ETLBox {
    public abstract class GenericTask : ITask
    {
        private string _taskType;
        public virtual string TaskType { get => String.IsNullOrEmpty(_taskType) ? this.GetType().Name : _taskType;
            set => _taskType = value; }
        public virtual string TaskName { get; set; } = "N/A";
        public NLog.Logger NLogger { get; set; } = CF.ControlFlow.GetLogger();

        public virtual void Execute()
        {
            throw new Exception("Not implemented!");
        }

        public virtual IConnectionManager ConnectionManager { get; set; }

        internal virtual IConnectionManager DbConnectionManager
        {
            get
            {
                if (ConnectionManager == null)
                    return (IConnectionManager)ControlFlow.ControlFlow.CurrentDbConnection;
                else
                    return (IConnectionManager)ConnectionManager;
            }
        }

        public ConnectionManagerType ConnectionType => ConnectionManagerSpecifics.GetType(this.DbConnectionManager);
        public string QB => ConnectionManagerSpecifics.GetBeginQuotation(this.ConnectionType);
        public string QE => ConnectionManagerSpecifics.GetEndQuotation(this.ConnectionType);

        public bool _disableLogging;
        public virtual bool DisableLogging
        {
            get
            {
                if (ControlFlow.ControlFlow.DisableAllLogging == false)
                    return _disableLogging;
                else
                    return ControlFlow.ControlFlow.DisableAllLogging;
            }
            set
            {
                _disableLogging = value;
            }
        }

        private string _taskHash;


        public virtual string TaskHash
        {
            get
            {
                if (_taskHash == null)
                    return HashHelper.Encrypt_Char40(this);
                else
                    return _taskHash;
            }
            set
            {
                _taskHash = value;
            }
        }
        internal virtual bool HasName => !String.IsNullOrWhiteSpace(TaskName);

        public GenericTask()
        { }

        public GenericTask(ITask callingTask)
        {
            TaskName = callingTask.TaskName;
            TaskHash = callingTask.TaskHash;
            ConnectionManager = callingTask.ConnectionManager;
            TaskType = callingTask.TaskType;
            DisableLogging = callingTask.DisableLogging;
        }
    }
}
