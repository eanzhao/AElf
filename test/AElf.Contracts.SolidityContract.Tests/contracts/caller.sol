pragma solidity ^0.8.20;

contract Caller {

    function caller() public view returns (address){
        return msg.sender;
    }
}