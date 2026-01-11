create table jammydatabase (id text PRIMARY KEY, name text);
CREATE TABLE jammylabel (id TEXT PRIMARY KEY, dbid text, name text, address uint);
CREATE TABLE jammytype (id TEXT PRIMARY KEY, dbid text, type int, address uint, size uint);
