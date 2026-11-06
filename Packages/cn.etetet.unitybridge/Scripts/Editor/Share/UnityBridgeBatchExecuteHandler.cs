namespace ET
{
    /// <summary>
    /// UnityBridge 批量顺序执行多条子命令的处理器（BatchExecute），支持遇错即停（StopOnError）或全量执行汇总。
    /// </summary>
    internal sealed class UnityBridgeBatchExecuteHandler : AUnityBridgeHandler<BatchExecuteRequest, BatchExecuteResponse>
    {
        protected override async ETTask<IResponse> Run(BatchExecuteRequest command)
        {
            BatchExecuteResponse response = BatchExecuteResponse.Create();
            if (command.Commands.Count == 0)
            {
                response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                response.Message = "Commands cannot be empty";
                return response;
            }

            for (int i = 0; i < command.Commands.Count; ++i)
            {
                string commandJson = command.Commands[i];
                BridgeBatchStepResult step = BridgeBatchStepResult.Create();
                step.Name = $"Step{i + 1}";
                step.Command = commandJson;

                if (string.IsNullOrWhiteSpace(commandJson))
                {
                    step.Error = UnityBridgeErrorCode.InvalidCommandLine;
                    step.Message = "batch command json is empty";
                    response.Results.Add(step);
                    response.Failed++;
                    if (command.StopOnError)
                    {
                        break;
                    }

                    continue;
                }

                if (!UnityBridgeEditorDispatcher.TryDeserializePersistedCommand(commandJson, out IRequest childCommand, out string parseError))
                {
                    step.Error = UnityBridgeErrorCode.InvalidCommandLine;
                    step.Message = parseError;
                    response.Results.Add(step);
                    response.Failed++;
                    if (command.StopOnError)
                    {
                        break;
                    }

                    continue;
                }

                childCommand.RpcId = command.RpcId;
                IResponse childResponse = await UnityBridgeEditorDispatcher.Dispatch(childCommand);
                step.Name = childCommand.GetType().Name;
                step.Error = childResponse?.Error ?? UnityBridgeErrorCode.HandlerFail;
                step.Message = childResponse?.Message ?? string.Empty;
                response.Results.Add(step);

                if (step.Error != 0)
                {
                    response.Failed++;
                    if (command.StopOnError)
                    {
                        break;
                    }
                }
            }

            response.Count = response.Results.Count;
            response.Completed = response.Failed == 0;
            if (!response.Completed)
            {
                response.Error = UnityBridgeErrorCode.HandlerFail;
                response.Message = $"{response.Failed} batch command(s) failed";
            }

            return response;
        }
    }
}
