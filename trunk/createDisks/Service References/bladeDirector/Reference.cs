﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace createDisks.bladeDirector {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName="bladeDirector.servicesSoap")]
    public interface servicesSoap {
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ListNodes", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        string ListNodes();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ListNodes", ReplyAction="*")]
        System.Threading.Tasks.Task<string> ListNodesAsync();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/getBladesByAllocatedServer", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        string getBladesByAllocatedServer(string NodeIP);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/getBladesByAllocatedServer", ReplyAction="*")]
        System.Threading.Tasks.Task<string> getBladesByAllocatedServerAsync(string NodeIP);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/RequestNode", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        string RequestNode(string NodeIP);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/RequestNode", ReplyAction="*")]
        System.Threading.Tasks.Task<string> RequestNodeAsync(string NodeIP);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/GetBladeStatus", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        string GetBladeStatus(string NodeIP);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/GetBladeStatus", ReplyAction="*")]
        System.Threading.Tasks.Task<string> GetBladeStatusAsync(string NodeIP);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/releaseBlade", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        string releaseBlade(string NodeIP);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/releaseBlade", ReplyAction="*")]
        System.Threading.Tasks.Task<string> releaseBladeAsync(string NodeIP);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/forceBladeAllocation", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        string forceBladeAllocation(string NodeIP, string newOwner);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/forceBladeAllocation", ReplyAction="*")]
        System.Threading.Tasks.Task<string> forceBladeAllocationAsync(string NodeIP, string newOwner);
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface servicesSoapChannel : createDisks.bladeDirector.servicesSoap, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class servicesSoapClient : System.ServiceModel.ClientBase<createDisks.bladeDirector.servicesSoap>, createDisks.bladeDirector.servicesSoap {
        
        public servicesSoapClient() {
        }
        
        public servicesSoapClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public servicesSoapClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public servicesSoapClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public servicesSoapClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        public string ListNodes() {
            return base.Channel.ListNodes();
        }
        
        public System.Threading.Tasks.Task<string> ListNodesAsync() {
            return base.Channel.ListNodesAsync();
        }
        
        public string getBladesByAllocatedServer(string NodeIP) {
            return base.Channel.getBladesByAllocatedServer(NodeIP);
        }
        
        public System.Threading.Tasks.Task<string> getBladesByAllocatedServerAsync(string NodeIP) {
            return base.Channel.getBladesByAllocatedServerAsync(NodeIP);
        }
        
        public string RequestNode(string NodeIP) {
            return base.Channel.RequestNode(NodeIP);
        }
        
        public System.Threading.Tasks.Task<string> RequestNodeAsync(string NodeIP) {
            return base.Channel.RequestNodeAsync(NodeIP);
        }
        
        public string GetBladeStatus(string NodeIP) {
            return base.Channel.GetBladeStatus(NodeIP);
        }
        
        public System.Threading.Tasks.Task<string> GetBladeStatusAsync(string NodeIP) {
            return base.Channel.GetBladeStatusAsync(NodeIP);
        }
        
        public string releaseBlade(string NodeIP) {
            return base.Channel.releaseBlade(NodeIP);
        }
        
        public System.Threading.Tasks.Task<string> releaseBladeAsync(string NodeIP) {
            return base.Channel.releaseBladeAsync(NodeIP);
        }
        
        public string forceBladeAllocation(string NodeIP, string newOwner) {
            return base.Channel.forceBladeAllocation(NodeIP, newOwner);
        }
        
        public System.Threading.Tasks.Task<string> forceBladeAllocationAsync(string NodeIP, string newOwner) {
            return base.Channel.forceBladeAllocationAsync(NodeIP, newOwner);
        }
    }
}
