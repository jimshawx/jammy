create table database (
	id text primary key,
	name text unique not null,
	time real not null
);
create index database_id on database (id);
create unique index database_name_unique on database (name);

create table label (
	id text primary key,
	dbid text not null,
	name text not null,
	address uint not null,
	time real not null,
	foreign key(dbid) references database (id)
);
create index label_id on label (id);
create index label_dbid on label (dbid);
create index label_address on label (address);

create table type (id text primary key,
	dbid text,
	type int,
	address uint,
	size uint,
	time real not null,
	foreign key(dbid) references database (id)
);
create index type_id on type (id);
create index type_dbid on type (dbid);
create index type_address on type (address);

create table header (
	id text primary key,
	dbid text,
	address uint,
	time real not null,
	foreign key(dbid) references database (id)
);
create index header_id on header (id);
create index header_dbid on header (dbid);

create table headerline (
	id text primary key,
	headerid text not null,
	line uint not null,
	text text not null,
	foreign key(headerid) references header (id)
);
create index headerline_id on headerline (id);

create table comment (
	id text primary key,
	dbid text,
	address uint not null,
	text text not null,
	time real not null,
	foreign key(dbid) references database (id)
);
create index comment_id on comment (id);
create index comment_dbid on comment (dbid);
create index comment_address on comment (address);

create table memtype (
	id text primary key,
	dbid text,
	type int not null,
	address uint not null,
	size uint not null,
	time real not null,
	foreign key(dbid) references database (id)
);
create index memtype_id on memtype (id);
create index memtype_dbid on memtype (dbid);
create index memtype_address on memtype (address);
