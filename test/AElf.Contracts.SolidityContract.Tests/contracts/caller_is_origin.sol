pragma solidity ^0.8.0;

contract CallerIsOrigin {
    function callerIsOrigin() public view returns (int) {
        return caller_is_origin(msg.sender);
    }
}