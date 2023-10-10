pragma solidity ^0.8.20;

contract Now {

    function now() public view returns (uint){
        return block.timestamp;
    }
}