DB TeXML is a programming language and IDE designed to rapidly generate backend libraries or scripts based on information schema structures of the targeted database. Sql Server, MySql, Postgres and Oracle are supported.

I wrote this code originally around 2010-2015, it started as a project in Python for rapidly building scripts to run against databases based on their information schemas to produce a set of files based on templates. 

Eventually I reworked the project in WPF with a GUI for writing code and integration for several databases that I was working with at the time. 

The code was written in such a way that new programming keywords could be inserted directly into the language by making use of C# reflection, this was done at the time out of curiosity and for fun to see if I could make this happen.

The primary reason I needed it was because my company at the time used a fairly not well known database in many projects called Ingres so there wasn't a lot of reusable integrations for data access layers in backend code, this language would allow me to write that code in just a few minutes or hours at most.
