drop table classes;
create table classes (
	class_id integer not null ,
	class_name varchar(64)  ,
	create_date timestamp without time zone  
	,primary key (class_id)
);

drop table schedule;
create table schedule (
	student_id integer not null ,
	class_id integer not null 
	,primary key (student_id, class_id)
);

drop table students;
create table students (
	student_id integer not null ,
	student_name varchar(64)  ,
	create_date timestamp without time zone  
	,primary key (student_id)
);

drop table alltypes;
create table alltypes (
	type01 bigint,
	type02 bigserial,
	type03 bit(8),
	type04 bit varying(8),
	type05 boolean,
	type06 box,
	type07 bytea,
	type08 character(8),
	type09 character varying(8),
	type10 cidr,
	type11 circle,
	type12 date,
	type13 double precision,
	type14 inet,
	type15 integer,
	type16 interval SECOND (8),
	type17 json,
	type18 line,
	type19 lseg,
	type20 macaddr,
	type21 money,
	type22 numeric (8, 8),
	type23 path,
	type24 point,
	type25 polygon,
	type26 real,
	type27 smallint,
	type28 smallserial,
	type29 serial,
	type30 text,
	type31 time(8) without time zone,
	type32 time(8) with time zone,
	type33 timestamp(8) without time zone,
	type34 timestamp(8) with time zone,
	type35 tsquery,
	type36 tsvector,
	type37 txid_snapshot,
	type38 uuid,
	type39 xml
);