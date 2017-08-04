# C# Driver for Oracle NoSQL Distributed Database


C# driver performs basic CRUD operations on Oracle NoSQL database.
Also supports asynchronus index-based search operation.

```
  |=====================|
  | C# user application |                 |==============|
  |  ===============--  | <--- Thrift --->| Proxy Server |             |==============|
  |     C# Driver       |                 |   (Java)     |<--- RMI --->| Oracle NoSQL |
  |=====================|                 |==============|             | Distribued   |
             |                                                         | Database     |
             |                                                         |==============|
       ****************
       * You are here *
       ****************


```


## Build

   The driver is built using `Nant` build tool.
   You will also need `msbuild` compiler, `thrift` compiler and `nuget` package manager.
   Once these tools are installed, edit `build.properties` to specify
   executable paths of these tools. 
   Then, execute `nant`
   
     $ nant
     
   This command builds the `driver.dll`
   
## Test
   
 To test the driver, you need Oracle NoSQL database installed.
 
   * Set `kv.install.dir` in `build.property`.
   * Start a database with a single node 
       
         $ nant start.store
         
   * Run all the tests
    
         $ nant run.test   